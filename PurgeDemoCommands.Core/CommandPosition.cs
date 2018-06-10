using System.Collections.Generic;

namespace PurgeDemoCommands.Core
{
    public class CommandPositions
    {
        public long MinimumIndex { get; set; }
        public IList<CommandPosition> Positions { get; set; }
        public int Count { get { return Positions.Count; }}
    }
    public class CommandPosition
    {
        public long Index { get; set; }
        public int NumberOfBytes { get; set; }
        public int Tick { get; set; }
        public bool IsConsoleCommand { get; set; }
    }
}