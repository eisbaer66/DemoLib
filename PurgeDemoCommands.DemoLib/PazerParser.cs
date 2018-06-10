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

        public async Task<CommandPositions> ReadDemo(string filename)
        {
            int tick = 0;
            long index = -1;
            int length = 0;
            bool readingConsoleCommand = false;
            DemoReader demo = ParseDemo(filename);

            long minIndex = demo.Commands.FirstOrDefault(c => c.Type == DemoCommandType.dem_stringtables).IndexEnd;

            List<CommandPosition> positions = demo.Commands
                .Select(c =>
                {
                    var paketCommand = c as TimestampedDemoCommand;
                    if (paketCommand == null)
                        return null;

                    int paketTick = paketCommand.Tick;
                    if (paketTick == 0 && tick > 0)
                        paketTick = tick;

                    bool isConsoleCommand = paketCommand.Type == DemoCommandType.dem_consolecmd;
                    bool changesType = isConsoleCommand ^ readingConsoleCommand; //XOR
                    bool changesTick = paketTick != tick;

                    if (!changesType && !changesTick)
                    {
                        length += (int) (c.IndexEnd - c.IndexStart);
                        return null;
                    }

                    var pos = CreatePos(readingConsoleCommand, index, length, tick);

                    index = paketCommand.IndexStart;
                    length = (int) (c.IndexEnd - c.IndexStart);
                    tick = paketTick;
                    readingConsoleCommand = isConsoleCommand;

                    if (pos.Index < 0)
                        return null;
                    return pos;
                })
                .Where(c => c != null)
                .ToList();

            return new CommandPositions
            {
                MinimumIndex = minIndex,
                Positions = positions,
            };
        }

        private static CommandPosition CreatePos(bool isConsoleCommand, long index, int length, int tick)
        {
            var pos = new CommandPosition
            {
                Index = index,
                NumberOfBytes = length,
                Tick = tick,
                IsConsoleCommand = isConsoleCommand,
            };
            return pos;
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