using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace PurgeDemoCommands.Specs
{
    static class TableExtensions
    {
        public static IEnumerable<string[]> AllRows(this Table table)
        {
            yield return table.Header.ToArray();

            foreach (TableRow row in table.Rows)
            {
                yield return row.Values.ToArray();
            }
        }
    }
}