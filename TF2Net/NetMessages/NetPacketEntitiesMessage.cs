﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BitSet;
using TF2Net.Data;

namespace TF2Net.NetMessages
{
	[DebuggerDisplay("{Description, nq}")]
	public class NetPacketEntitiesMessage : INetMessage
	{
		const int DELTA_INDEX_BITS = 32;
		const int DELTA_SIZE_BITS = 20;

		public uint MaxEntries { get; set; }
		public uint UpdatedEntries { get; set; }
		public bool IsDelta { get; set; }
		public bool UpdateBaseline { get; set; }
		public BaselineIndex? Baseline { get; set; }
		public int? DeltaFrom { get; set; }

		public BitStream Data { get; set; }

		public string Description
		{
			get
			{
				return string.Format("svc_PacketEntities: delta {0}, max {1}, changed {2}, {3} bytes {4}",
					DeltaFrom, MaxEntries, UpdatedEntries,
					UpdateBaseline ? " BL update," : "",
					BitInfo.BitsToBytes(Data.Length));
			}
		}

		public void ReadMsg(BitStream stream)
		{
			MaxEntries = stream.ReadUInt(SourceConstants.MAX_EDICT_BITS);

			IsDelta = stream.ReadBool();
			if (IsDelta)
				DeltaFrom = stream.ReadInt(DELTA_INDEX_BITS);

			Baseline = (BaselineIndex)stream.ReadByte(1);

			UpdatedEntries = stream.ReadUInt(SourceConstants.MAX_EDICT_BITS);

			ulong bitCount = stream.ReadULong(DELTA_SIZE_BITS);

			UpdateBaseline = stream.ReadBool();

			Data = stream.Subsection(stream.Cursor, stream.Cursor + bitCount);
			stream.Seek(bitCount, System.IO.SeekOrigin.Current);
		}

		static uint ReadUBitInt(BitStream stream)
		{
			uint ret = stream.ReadUInt(6);
			switch (ret & (16 | 32))
			{
				case 16:
				ret = (ret & 15) | (stream.ReadUInt(4) << 4);
				break;
				case 32:
				ret = (ret & 15) | (stream.ReadUInt(8) << 4);
				break;
				case 48:
				ret = (ret & 15) | (stream.ReadUInt(32 - 4) << 4);
				break;
			}
			return ret;
		}

		static uint ReadUBitVar(BitStream stream)
		{
			switch (stream.ReadByte(2))
			{
				case 0:		return stream.ReadUInt(4);
				case 1:		return stream.ReadUInt(8);
				case 2:		return stream.ReadUInt(12);
				case 3:		return stream.ReadUInt(32);
			}

			throw new Exception("Should never get here...");
		}

		public void ApplyWorldState(WorldState ws)
		{
			if (ws.SignonState.State == ConnectionState.Spawn)
			{
				if (!IsDelta)
				{
					// We are done with signon sequence.
					ws.SignonState.State = ConnectionState.Full;
				}
				else
					throw new InvalidOperationException("eceived delta packet entities while spawing!");
			}

			ClientFrame newFrame = new ClientFrame(ws.Tick);
			ClientFrame oldFrame = null;
			if (IsDelta)
			{
				if (ws.Tick == (ulong)DeltaFrom.Value)
					throw new InvalidDataException("Update self-referencing");

				oldFrame = ws.Frames.Single(f => f.ServerTick == (ulong)DeltaFrom.Value);
			}

			if (UpdateBaseline)
			{
				if (Baseline.Value == BaselineIndex.Baseline0)
				{
					ws.Baselines[(int)BaselineIndex.Baseline1] = ws.Baselines[(int)BaselineIndex.Baseline0];
					ws.Baselines[(int)BaselineIndex.Baseline0] = new List<Entity>();
				}
				else if (Baseline.Value == BaselineIndex.Baseline1)
				{
					ws.Baselines[(int)BaselineIndex.Baseline0] = ws.Baselines[(int)BaselineIndex.Baseline1];
					ws.Baselines[(int)BaselineIndex.Baseline1] = new List<Entity>();
				}
				else
					throw new ArgumentOutOfRangeException(nameof(Baseline));
			}

			Data.Seek(0, System.IO.SeekOrigin.Begin);
			
			int newEntity = -1;
			int oldEntity = -1;
			for (int i = 0; i < UpdatedEntries; i++)
			{
				// NextOldEntity
				if (oldFrame != null)
				{
					var nextSet = oldFrame.TransmitEntity.FindNextSetBit((uint)(oldEntity + 1));
					oldEntity = nextSet.HasValue ? (int)nextSet.Value : int.MaxValue;
				}
				else
				{
					oldEntity = int.MaxValue;
				}

				newEntity += 1 + (int)ReadUBitVar(Data);

				// Leave PVS flag
				if (!Data.ReadBool())
				{
					// Enter PVS flag
					if (Data.ReadBool())
					{
						Entity e = ReadEnterPVS(ws, Data, (uint)newEntity);
						
						ApplyEntityUpdate(e, Data);

						Debug.Assert(!ws.Entities.Any(x => x.Index == e.Index));
						ws.Entities.Add(e);
					}
					else
					{
						// Preserve/update
						Entity e = ws.Entities.Single(ent => ent.Index == newEntity);
						ApplyEntityUpdate(e, Data);
					}
				}
				else
				{
					if (Data.ReadBool())
						throw new NotImplementedException("Force delete");
					else
						throw new NotImplementedException("Leave PVS");

					var removed = ws.Entities.RemoveWhere(e => e.Index == newEntity);
					Debug.Assert(removed == 1);

					Data.Cursor++;
				}

				if (newEntity > oldEntity && (oldFrame == null || oldEntity > oldFrame.LastEntityIndex))
				{
					Debug.Assert(i == (UpdatedEntries - 1));
					break;
				}
			}
		}

		static Entity ReadEnterPVS(WorldState ws, BitStream stream, uint entityIndex)
		{
			uint serverClassID = stream.ReadUInt(ws.ClassBits);
			uint serialNumber = stream.ReadUInt(SourceConstants.NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS);

			Entity e = new Entity(entityIndex, serialNumber);
			e.Class = ws.ServerClasses[(int)serverClassID];
			e.NetworkTable = ws.SendTables.Single(st => st.NetTableName == e.Class.DatatableName);

			BitStream baseline = ws.StaticBaselines.SingleOrDefault(bl => bl.Key == e.Class).Value;
			if (baseline != null)
			{
				baseline.Cursor = 0;
				ApplyEntityUpdate(e, baseline);
				Debug.Assert((baseline.Length - baseline.Cursor) < 8);
			}

			return e;
		}

		static void ApplyEntityUpdate(Entity e, BitStream stream)
		{
			var guessProps = e.NetworkTable.SortedProperties.ToArray();

			int index = -1;
			while ((index = ReadFieldIndex(stream, index)) != -1)
			{
				Debug.Assert(index < guessProps.Length);
				Debug.Assert(index < SourceConstants.MAX_DATATABLE_PROPS);

				var prop = guessProps[index];

				var decoded = prop.Property.Decode(stream);

				e.Properties[prop.Property] = decoded;
				guessProps[index] = null;
			}
		}

		[DebuggerDisplay("[{HighestDepth,nq}] {Property,nq} = {Value}")]
		class Node
		{
			/// <summary>
			/// If we have multiple paths with the same depth.
			/// </summary>
			public List<Node> ChildNodes { get; set; }

			public int FieldIndex { get; set; }
			
			public SendProp Property { get; set; }

			public ulong BitsRead { get; set; }
			public object Value { get; set; }

			public BitStream Stream { get; set; }

			public IEnumerable<SendProp> RemainingProps { get; set; }

			public int HighestDepth
			{
				get
				{
					int highest = 0;

					foreach (var node in ChildNodes)
						highest = Math.Max(highest, node.HighestDepth + 1);

					return highest;
				}
			}
		}

		static int ReadFieldIndex(BitStream stream, int lastIndex)
		{
#if true
			if (!stream.ReadBool())
				return -1;
			
			var diff = ReadUBitVar(stream);
			return (int)(lastIndex + diff + 1);
#else
			if (stream.ReadBool())
				return lastIndex + 1;

			int ret = 0;
			if (stream.ReadBool())
			{
				ret = stream.ReadInt(3);  // read 3 bits
			}
			else
			{
				ret = stream.ReadInt(7); // read 7 bits
				switch (ret & (32 | 64))
				{
					case 32:
					ret = (ret & ~96) | stream.ReadInt(2) << 5;
					break;
					case 64:
					ret = (ret & ~96) | stream.ReadInt(4) << 5;
					break;
					case 96:
					ret = (ret & ~96) | stream.ReadInt(7) << 5;
					break;
				}
			}

			if (ret == 0xFFF)
			{ // end marker is 4095 for cs:go
				return -1;
			}

			return lastIndex + 1 + ret;
#endif
		}
	}
}
