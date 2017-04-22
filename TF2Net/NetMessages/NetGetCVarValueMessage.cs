using BitSet;
using TF2Net.Data;

namespace TF2Net.NetMessages
{
    internal class NetGetCVarValueMessage : INetMessage
    {
        public string Description { get; }
        public int Cookie { get; set; }
        public string CVarName { get; set; }

        public void ReadMsg(BitStream stream)
        {
            Cookie = stream.ReadInt();
            CVarName = stream.ReadCString();
        }


        public void ApplyWorldState(WorldState ws)
        {
            throw new System.NotImplementedException();
        }
    }
}
