using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoLib;
using DemoLib.Commands;
using PurgeDemoCommands.Core;
using PurgeDemoCommands.DemoLib.Logging;

namespace PurgeDemoCommands.DemoLib
{
    public class PazerParser : IParser
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private static int _commandTypeOffset = 4;

        public async Task<IList<CommandPosition>> ReadDemo(string filename)
        {
            int tick = 0;
            DemoReader demo = ParseDemo(filename);
            return demo.Commands
                .Select(c =>
                {
                    var paketCommand = c as DemoPacketCommand;
                    if (paketCommand != null)
                    {
                        tick = paketCommand.Tick;
                        return null;
                    }
                    var consoleCommand = c as DemoConsoleCommand;
                    if (consoleCommand == null)
                        return null;

                    return new CommandPosition
                    {
                        Index = consoleCommand.IndexStart + _commandTypeOffset,
                        NumberOfBytes = consoleCommand.IndexEnd - consoleCommand.IndexStart - _commandTypeOffset,
                        Tick = tick,
                    };
                })
                .Where(c => c != null)
                .ToList();
        }

        private static DemoReader ParseDemo(string filename)
        {
            Log.DebugFormat("reading demo from {Filename}", filename);

            return Parse(filename);
        }

        private static DemoReader Parse(string filename)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                return DemoReader.FromStream(stream);
            }
        }
    }
}