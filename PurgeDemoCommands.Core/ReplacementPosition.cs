using System.Linq;

namespace PurgeDemoCommands.Core
{
    public class ReplacementPosition
    {
        public long Index { get; set; }
        public byte[] Bytes { get; set; }
        public bool IsFree { get; set; }


        public static ReplacementPosition Nulls(long index, long numberOfBytes)
        {
            byte[] array = Enumerable.Range(1, (int)numberOfBytes).Select(i => (byte)0).ToArray();

            return new ReplacementPosition
            {
                Index = index,
                Bytes = array,
                IsFree = true,
            };
        }
    }
}