using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core.DemoEditActions
{
    [DebuggerDisplay("{Tick}:{Commands}")]
    class InsertDemoEditAction : IDemoEditAction
    {
        private const int CommandTypeLength = 1;
        private const int TickLength = 4;
        private const int CommandLengthLength = 4;
        private const int StringTerminatorLength = 1;

        public ITickInjection Injection { get; set; }
        public int Tick
        {
            get { return Injection.Tick; }
        }
        public string Commands
        {
            get { return Injection.Commands; }
        }

        public async Task Execute(FileStream readStream, FileStream writeStream)
        {
            byte[] bytes = GenerateBytes(Injection);
            await writeStream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static byte[] GenerateBytes(ITickInjection injection)
        {
            byte[] bytes = new byte[CommandTypeLength + TickLength + CommandLengthLength + injection.Commands.Length + StringTerminatorLength];

            using (MemoryStream writeStream = new MemoryStream(bytes))
            using (var writer = new BinaryWriter(writeStream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write((byte)4);
                writer.Write((injection.Tick));
                writer.Write(injection.Commands.Length + StringTerminatorLength);
                foreach (char c in injection.Commands)
                {
                    writer.Write(c);
                }

                writer.Write((byte)0);
            }

            return bytes;
        }
    }
}