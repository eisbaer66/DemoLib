using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core.DemoEditActions
{
    [DebuggerDisplay("{Tick}@{Index}")]
    public class CopyDemoEditAction : IDemoEditAction
    {
        public long Index { get; set; }
        public int Length { get; set; }
        public int Tick { get; set; }

        private readonly IBuffer _buffer;
        public CopyDemoEditAction(IBuffer buffer)
        {
            _buffer = buffer;
        }

        public async Task Execute(FileStream readStream, FileStream writeStream)
        {
            await CopyStream(readStream, writeStream);
        }

        private async Task CopyStream(FileStream readStream, FileStream writeStream)
        {
            readStream.Position = Index;
            await ReadStream(readStream);

            await writeStream.WriteAsync(_buffer.Array, 0, Length);
        }

        private async Task ReadStream(FileStream stream)
        {
            _buffer.EnsureLength(Length);

            await stream.ReadAsync(_buffer.Array, 0, Length);
        }
    }
}