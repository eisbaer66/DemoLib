using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoLib;
using DemoLib.Commands;
using Nito.AsyncEx;

namespace EditDemoCommands
{
    class Program
    {
        private static byte[] _buffer = new byte[100];

        static void Main(string[] args)
        {
            //AsyncContext.Run(RemoveConsoleCommands);
            AsyncContext.Run(InsertConsoleCommands);
        }

        private static async Task InsertConsoleCommands()
        {
            //string filename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi.dem";
            string filename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi_stripped.dem";
            string newFilename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi_added.dem";
            using (FileStream stream = File.OpenRead(filename))
            {
                DemoReader demo = DemoReader.FromStream(stream);

                //var demoConsoleCommands = demo.Commands.OfType<DemoSyncTickCommand>().Where(c => c.Tick == 0).ToList();
                //List<int> ticks = demo.Commands.OfType<TimestampedDemoCommand>().Select(c => c.Tick).Distinct().ToList();
                //IEnumerable<int> expectedTicks = Enumerable.Range(500, ticks[ticks.Count - 1]);
                //foreach (int expectedTick in expectedTicks)
                //{
                //    bool contains = ticks.Contains(expectedTick);
                //    if (!contains)
                //        Console.WriteLine("no commadn for tick" + expectedTick);
                //}

                if (File.Exists(newFilename))
                    File.Delete(newFilename);

                int tick = 650;
                DemoCommand command = demo.Commands.OfType<TimestampedDemoCommand>().LastOrDefault(c => c.Tick <= tick);

                long bytesBeforeCommand = command.IndexEnd;


                stream.Position = 0;

                using (FileStream writeStream = File.OpenWrite(newFilename))
                {
                    await CopyStream(bytesBeforeCommand, stream, writeStream);
                    
                    using (var writer = new BinaryWriter(writeStream, Encoding.ASCII, leaveOpen: true))
                    {
                        string commandText = "demo_timescale .5";

                        writer.Write((byte)4);
                        writer.Write(tick);
                        writer.Write(commandText.Length+1);
                        foreach (char c in commandText)
                        {
                            writer.Write(c);
                        }
                        writer.Write((byte)0);
                    }



                    await CopyStream(stream.Length - stream.Position, stream, writeStream);
                }
            }

            using (FileStream stream = File.OpenRead(newFilename))
            {
                DemoReader.FromStream(stream);
            }


            FileInfo fileInfo = new FileInfo(newFilename);
            long newSize = fileInfo.Length;
            FileInfo oldfileInfo = new FileInfo(filename);
            long oldSize = oldfileInfo.Length;
            long actualSizeReduction = oldSize - newSize;

            Console.WriteLine("old size: " + oldSize);
            Console.WriteLine("new size: " + newSize);
            Console.WriteLine("diff:     " + actualSizeReduction);

            Console.WriteLine("finished");
        }

        private static async Task RemoveConsoleCommands()
        {
            //string filename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi.dem";
            string filename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi.dem";
            string newFilename = "D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi_stripped.dem";
            long expectedSizeReduction;
            using (FileStream stream = File.OpenRead(filename))
            {
                var demoReader = DemoReader.FromStream(stream);
                var positions = demoReader.Commands
                    .OfType<DemoConsoleCommand>()
                    .Select(c => new { Start = c.IndexStart, End = c.IndexEnd })
                    .OrderBy(p => p.Start);


                var demoConsoleCommands = demoReader.Commands.OfType<DemoConsoleCommand>().Where(c => c.Tick != 0).ToList();
                var demoConsoleCommands2 = demoReader.Commands.OfType<DemoConsoleCommand>().Where(c => c.Tick == 0).ToList();



                expectedSizeReduction = positions.Select(p => p.End - p.Start).Sum();

                if (File.Exists(newFilename))
                    File.Delete(newFilename);

                stream.Position = 0;

                using (FileStream writeStream = File.OpenWrite(newFilename))
                {
                    foreach (var position in positions)
                    {
                        Console.WriteLine("processing console commands at position {0} to {1}", position.Start, position.End);

                        long diff = position.Start - stream.Position; 
                        if (diff > 0)
                            await CopyStream(diff, stream, writeStream);

                        Console.WriteLine("at position {0} after copying {1} bytes", stream.Position, diff);

                        diff = position.End - stream.Position;
                        await ReadStream((int)diff, stream);

                        Console.WriteLine("at position {0} after ignoring {1} bytes", stream.Position, diff);
                        //stream.Position = position.End;
                        //Console.WriteLine("at position {0}", stream.Position, diff);

                    }

                    long lengthTillEnd = stream.Length - stream.Position;
                    if (lengthTillEnd > 0)
                        await CopyStream(lengthTillEnd, stream, writeStream);
                }
            }



            FileInfo fileInfo = new FileInfo(newFilename);
            long newSize = fileInfo.Length;
            FileInfo oldfileInfo = new FileInfo(filename);
            long oldSize = oldfileInfo.Length;
            long actualSiteReduction = oldSize - newSize;

            Console.WriteLine("old size: " + oldSize);
            Console.WriteLine("new size: " + newSize);
            Console.WriteLine("diff:     " + actualSiteReduction);
            Console.WriteLine("diffdiff: " + Math.Abs(actualSiteReduction- expectedSizeReduction));

            Console.WriteLine("finished");
        }

        private static async Task CopyStream(long diff, FileStream stream, FileStream writStream)
        {
            int diffInt = (int) diff;
            await ReadStream(diffInt, stream);

            await writStream.WriteAsync(_buffer, 0, diffInt);
        }

        private static async Task<int> ReadStream(int diff, FileStream stream)
        {
            if (diff > _buffer.Length)
                _buffer = new byte[diff];

            await stream.ReadAsync(_buffer, 0, diff);
            return diff;
        }
    }
}
