using System.Diagnostics;
using BitSet;
using TF2Net.Data;

namespace TF2Net.NetMessages
{
	[DebuggerDisplay("{Description, nq}")]
	public class NetVoiceInitMessage : INetMessage
	{
		public string VoiceCodec { get; set; }
		public byte Quality { get; set; }

		public string Description => string.Format("svc_VoiceInit: codec \"{0}\", quality {1} samplerate {2}", VoiceCodec, Quality, SampleRate);

	    public void ReadMsg(BitStream stream)
		{
			VoiceCodec = stream.ReadCString();
			Quality = stream.ReadByte();
		    SampleRate = stream.ReadUInt(16);
        }

	    public uint SampleRate { get; set; }

        public void ApplyWorldState(WorldState ws)
		{
			//throw new NotImplementedException();
		}
	}
}
