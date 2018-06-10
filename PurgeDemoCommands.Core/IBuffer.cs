namespace PurgeDemoCommands.Core
{
    public interface IBuffer
    {
        byte[] Array { get; }
        void EnsureLength(int length);
    }

    class Buffer : IBuffer
    {
        public byte[] Array { get; private set; } = new byte[1024];

        public void EnsureLength(int length)
        {
            if (length > Array.Length)
                Array = new byte[length];
        }
    }
}