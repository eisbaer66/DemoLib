using System.Diagnostics;
using System.IO;
using System.Text;

namespace DemoLib.Commands
{
    [DebuggerDisplay("{DebuggerDisplayAttributeValue,nq}")]
	public sealed class DemoConsoleCommand : TimestampedDemoCommand
	{
		public string Command { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string DebuggerDisplayAttributeValue
		{
			get { return Command.Replace('"', '\''); }
		}

		public DemoConsoleCommand(Stream input) : base(input)
		{
			Type = DemoCommandType.dem_consolecmd;
            
			using (BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true))
				Command = new string(reader.ReadChars(reader.ReadInt32())).TrimEnd('\0');
        }
    }
}
