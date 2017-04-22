using System.Diagnostics;
using BitSet;
using TF2Net.Data;

namespace TF2Net.NetMessages
{
    [DebuggerDisplay("{Description, nq}")]
    internal class NetTerrainModMessage : INetMessage
    {
        public string Description => "svc_terrainmod";

        public void ReadMsg(BitStream stream)
        {
        }

        public void ApplyWorldState(WorldState ws)
        {
        }
    }
}