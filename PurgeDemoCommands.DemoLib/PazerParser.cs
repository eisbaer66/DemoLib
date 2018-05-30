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
            DemoReader demo = ParseDemo(filename);
            return demo.Commands
                .OfType<DemoConsoleCommand>()
                .Select(c => new CommandPosition
                {
                    Index = c.IndexStart + _commandTypeOffset,
                    NumberOfBytes = c.IndexEnd - c.IndexStart - _commandTypeOffset,
                })
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