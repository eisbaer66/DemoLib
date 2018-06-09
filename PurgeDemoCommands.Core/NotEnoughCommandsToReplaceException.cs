using System;
using System.Collections.Generic;

namespace PurgeDemoCommands.Core
{
    public class NotEnoughCommandsToReplaceException : Exception
    {
        private readonly int _tick;
        private readonly IDictionary<int, List<string>> _queue;

        public NotEnoughCommandsToReplaceException(int tick, IDictionary<int, List<string>> queue)
            :base(string.Format("the demo does not contain enough console commands to inject commands intended for tick {0}", tick))
        {
            _tick = tick;
            _queue = queue;
        }
    }
}