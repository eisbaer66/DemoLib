using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoLib;
using DemoLib.Commands;

namespace AnalyzeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandTypeOverview("D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\purged\\5-Nice-Work-By-Kenpachi.dem", "5-Nice-Work-By-Kenpachi_purged.txt");
            CommandTypeOverview("D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf\\5-Nice-Work-By-Kenpachi.dem", "5-Nice-Work-By-Kenpachi_pure.txt");
        }

        private static void CommandTypeOverview(string demoFilename, string outputFilename)
        {
            DemoReader demo;
            using (var stream = File.OpenRead(demoFilename))
            {
                demo = DemoReader.FromStream(stream);
            }

            using (var stream = File.OpenWrite(outputFilename))
            using (var writer = new StreamWriter(stream))
            {
                foreach (var command in demo.Commands)
                {
                    string text = command.Type.ToString();

                    TimestampedDemoCommand timestamp = command as TimestampedDemoCommand;
                    if (timestamp != null)
                        text += "\t" + timestamp.Tick;
                    DemoPacketCommand paket = command as DemoPacketCommand;
                    if (paket != null)
                        text += "\t" + paket.SequenceIn + "\t" + paket.SequenceOut + "\t" + string.Join(", ", paket.Messages.Select(m => m.Description));

                    writer.WriteLine(text);
                }
            }
        }
    }
}
