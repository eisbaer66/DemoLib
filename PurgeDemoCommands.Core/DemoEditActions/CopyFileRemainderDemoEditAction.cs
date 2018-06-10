using System.IO;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core.DemoEditActions
{
    class CopyFileRemainderDemoEditAction : IDemoEditAction
    {
        private readonly IBuffer _buffer;
        public CopyFileRemainderDemoEditAction(IBuffer buffer)
        {
            _buffer = buffer;
        }

        public async Task Execute(FileStream readStream, FileStream writeStream)
        {
            int length = (int)(readStream.Length - readStream.Position);
            if (length <= 0)
                return;

            _buffer.EnsureLength(length);
            await readStream.ReadAsync(_buffer.Array, 0, length);
            await writeStream.WriteAsync(_buffer.Array, 0, length);
        }
    }
}