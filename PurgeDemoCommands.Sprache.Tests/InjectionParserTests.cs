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
                //from span in Span.WithoutAny(c => c == ';' || c == '\r' || c == '\n').Many()
                from span in Character.ExceptIn(';', '\r', '\n').Many()
                select new string(span);

            string s = commandTextParser.Parse("test");

            Assert.AreEqual("test", s);
        }
        [DataTestMethod]
        [DataRow(">0 demo_gototick 500")]
        [DataRow(">0 demo_gototick 500 ")]
        [DataRow(" >0 demo_gototick 500")]
        [DataRow(" >0 demo_gototick 500 ")]
        [DataRow(">0 demo_gototick 500\r\n")]
        [DataRow("\r\n>0 demo_gototick 500")]
        [DataRow("\r\n>0 demo_gototick 500\r\n")]
        public void ParsesSingleCommand(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Dir = TickIterationDirection.Forward, Tick = 0, Commands = new List<string>{"demo_gototick 500"}}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }

        [DataTestMethod]
        [DataRow(">0 demo_gototick 500;demo_timescale .5")]
        [DataRow(">0 demo_gototick 500;demo_timescale .5 ")]
        [DataRow(" >0 demo_gototick 500;demo_timescale .5")]
        [DataRow(" >0 demo_gototick 500; demo_timescale .5 ")]
        [DataRow(">0 demo_gototick 500;demo_timescale .5\r\n")]
        [DataRow("\r\n>0 demo_gototick 500;demo_timescale .5")]
        [DataRow("\r\n>0 demo_gototick 500;demo_timescale .5\r\n")]
        public void ParsesTwoCommandsInSameItem(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Dir = TickIterationDirection.Forward, Tick = 0, Commands = new List<string>{"demo_gototick 500", "demo_timescale .5"}}
            };
            Assert.That.ItemsAreEqual(expectedItems, items);
        }

        [DataTestMethod]
        [DataRow(">0 demo_gototick 500\r\n<500 demo_timescale .5")]
        [DataRow(">0 demo_gototick 500\r\n<500 demo_timescale .5 ")]
        [DataRow(" >0 demo_gototick 500\r\n<500 demo_timescale .5")]
        [DataRow(" >0 demo_gototick 500\r\n <500 demo_timescale .5 ")]
        [DataRow(" >0 demo_gototick 500 \r\n<500 demo_timescale .5 ")]
        [DataRow(" >0 demo_gototick 500 \r\n <500 demo_timescale .5 ")]
        [DataRow(">0 demo_gototick 500\r\n<500 demo_timescale .5\r\n")]
        [DataRow("\r\n>0 demo_gototick 500\r\n<500 demo_timescale .5")]
        [DataRow("\r\n>0 demo_gototick 500\r\n<500 demo_timescale .5\r\n")]
        public void ParsesTwoCommandsInSeperateItems(string text)
        {
            Result<IEnumerable<TickConfigItem>> items = new InjectionParser().ParseFrom(text);

            IEnumerable<TickConfigItem> expectedItems = new []
            {
                new TickConfigItem {Dir = TickIterationDirection.Forward, Tick = 0, Commands = new List<string>{"demo_gototick 500"}},
                new TickConfigItem {Dir = TickIterationDirection.Backward, Tick = 500, Commands = new List<string>{"demo_timescale .5"}}
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

                Assert.AreEqual(expectedItem.Dir, actualItem.Dir, "direction of {0}. item does not match", itemIndex);
                Assert.AreEqual(expectedItem.Tick, actualItem.Tick, "tick of {0}. item does not match", itemIndex);

                Assert.AreEqual(expectedItem.Commands.Count, actualItem.Commands.Count, "command count of {0}. item does not match", itemIndex);
                for (int commandIndex = 0; commandIndex < expectedItem.Commands.Count; commandIndex++)
                {
                    string expectedCommand = expectedItem.Commands[commandIndex];
                    string actualICommand = actualItem.Commands[commandIndex];

                    Assert.AreEqual(expectedCommand, actualICommand, "command at index {1} of {0}. item does not match", itemIndex, commandIndex);
                }
            }

        }
    }
}
