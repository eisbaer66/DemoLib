using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurgeDemoCommands.AhoCorasick;
using PurgeDemoCommands.Core;
using PurgeDemoCommands.DemoLib;

namespace PurgeDemoCommands.CompareTests
{
    [TestClass]
    public class AhoVsPazerTests
    {
        public static IEnumerable<object[]> ParsersCreateSameOutputInput
        {
            get
            {
                return Directory
                    .GetFiles("D:\\Games\\Steam\\SteamApps\\common\\Team Fortress 2\\tf", "*.dem", SearchOption.AllDirectories)
                    .Select(f => new object[1] {f});
            }
        }


        [DynamicData("ParsersCreateSameOutputInput")]
        [DataTestMethod]
        public async Task ParsersCreateSameOutput(string filename)
        {
            string[] commands = GetCommandsFromFile("D:\\Dokumente\\GitHub\\PurgeDemoCommands\\PurgeDemoCommands\\bin\\Debug\\commandlist.txt");

            AhoCorasickParser ahoCorasickParser = new AhoCorasickParser(commands);
            IList<CommandPosition> ahoCorasickPositions = await ahoCorasickParser.ReadDemo(filename);

            PazerParser pazerParser = new PazerParser();
            IList<CommandPosition> pazerPositions = await pazerParser.ReadDemo(filename);


            Assert.AreEqual(pazerPositions.Count, ahoCorasickPositions.Count, "Count does not match");

            for (int i = 0; i < pazerPositions.Count; i++)
            {
                CommandPosition pazerPosition = pazerPositions[i];
                CommandPosition ahoCorasickPosition = ahoCorasickPositions[i];

                Assert.AreEqual(pazerPosition.Index, ahoCorasickPosition.Index, "Index does not match");
                Assert.AreEqual(pazerPosition.NumberOfBytes, ahoCorasickPosition.NumberOfBytes, "NumberOfBytes does not match");
            }
        }

        private static string[] GetCommandsFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (!File.Exists(path))
                return new string[0];

            return File.ReadAllLines(path);
        }
    }
}
