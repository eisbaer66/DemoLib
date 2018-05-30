using System.IO;
using DemoLib;
using PurgeDemoCommands.Core;
using PurgeDemoCommands.DemoLib.Logging;

namespace PurgeDemoCommands.DemoLib
{
    public class IsParsableByPazer : ITest
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public void Run(string filename)
        {
            Log.DebugFormat("testing demo {Filename}", filename);

            Parse(filename);

            Log.DebugFormat("test successfull for demo {Filename}", filename);
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