using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Superpower;
using Superpower.Parsers;

namespace PurgeDemoCommands.Sprache.Tests
{
    [TestClass]
    public class InjectionParserTests
    {
        [TestMethod]
        public void Test()
        {

            var commandTextParser =
                from span in Character.ExceptIn(';', '\r', '\n').Many()
                select new string(span);

            string s = commandTextParser.Parse("test");

            Assert.AreEqual("test", s);
        }

        [TestMethod]
        public void TestCase()
        {
            string text = "0 demo_gototick 350 0 1\r\n1000 demo_timescale 0.5";
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Tick = 0, Commands ="demo_gototick 350 0 1"},
                new TickConfigItem {Tick = 1000, Commands ="demo_timescale 0.5"}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }

        [DataTestMethod]
        [DataRow("0 demo_gototick 500")]
        [DataRow("0 demo_gototick 500 ")]
        [DataRow(" 0 demo_gototick 500")]
        [DataRow(" 0 demo_gototick 500 ")]
        [DataRow("0 demo_gototick 500\r\n")]
        [DataRow("\r\n0 demo_gototick 500")]
        [DataRow("\r\n0 demo_gototick 500\r\n")]
        public void ParsesSingleCommand(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Tick = 0, Commands ="demo_gototick 500"}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }

        [DataTestMethod]
        [DataRow("0 demo_gototick 500;demo_timescale .5")]
        [DataRow("0 demo_gototick 500;demo_timescale .5 ")]
        [DataRow(" 0 demo_gototick 500;demo_timescale .5")]
        [DataRow(" 0 demo_gototick 500;demo_timescale .5 ")]
        [DataRow("0 demo_gototick 500;demo_timescale .5\r\n")]
        [DataRow("\r\n0 demo_gototick 500;demo_timescale .5")]
        [DataRow("\r\n0 demo_gototick 500;demo_timescale .5\r\n")]
        public void ParsesTwoCommandsInSameItem(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Tick = 0, Commands ="demo_gototick 500;demo_timescale .5"}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }

        [DataTestMethod]
        [DataRow("0 demo_gototick 500\r\n500 demo_timescale .5")]
        [DataRow("0 demo_gototick 500\r\n500 demo_timescale .5 ")]
        [DataRow(" 0 demo_gototick 500\r\n500 demo_timescale .5")]
        [DataRow(" 0 demo_gototick 500\r\n 500 demo_timescale .5 ")]
        [DataRow(" 0 demo_gototick 500 \r\n500 demo_timescale .5 ")]
        [DataRow(" 0 demo_gototick 500 \r\n 500 demo_timescale .5 ")]
        [DataRow("0 demo_gototick 500\r\n500 demo_timescale .5\r\n")]
        [DataRow("\r\n0 demo_gototick 500\r\n500 demo_timescale .5")]
        [DataRow("\r\n0 demo_gototick 500\r\n500 demo_timescale .5\r\n")]
        public void ParsesTwoCommandsInSeperateItems(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Tick = 0, Commands = "demo_gototick 500"},
                new TickConfigItem {Tick = 500, Commands = "demo_timescale .5"}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }
    }


    public static class AssertExctensions
    {
        public static void ItemsAreEqual(this Assert assert, IEnumerable<TickConfigItem> expected, Result<IEnumerable<TickConfigItem>> actual)
        {
            Assert.IsTrue(actual.Success, actual.Message);

            ItemsAreEqual(assert, expected.ToList(), actual.Items.ToList());
        }
        public static void ItemsAreEqual(this Assert assert, IList<TickConfigItem> expected, IList<TickConfigItem> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }
            
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Count, actual.Count, "item count does not match");

            for (int itemIndex = 0; itemIndex < expected.Count; itemIndex++)
            {
                TickConfigItem expectedItem = expected[itemIndex];
                TickConfigItem actualItem = actual[itemIndex];

                Assert.AreEqual(expectedItem.Tick, actualItem.Tick, "tick of {0}. item does not match", itemIndex);

                Assert.AreEqual(expectedItem.Commands, actualItem.Commands, "commands of {0}. item does not match", itemIndex);
            }

        }
    }
}
