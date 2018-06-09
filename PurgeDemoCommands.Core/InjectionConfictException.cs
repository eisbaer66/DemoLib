using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PurgeDemoCommands.Core
{
    internal class InjectionConfictException : Exception
    {
        private IList<ReplacementPosition> position;
        private IDictionary<int, List<string>> queue;
        private int tick;
        private readonly int _originalTick;

        public InjectionConfictException(IList<ReplacementPosition> positions, IDictionary<int, List<string>> queue, int tick, int originalTick)
            :base(string.Format("confilct on tick {0} (originally intended for tick {1}) already contains {2}", tick, originalTick, string.Join(", ", positions.Select(p => Encoding.ASCII.GetString(p.Bytes)).ToArray())))
        {
            this.position = positions;
            this.queue = queue;
            this.tick = tick;
            _originalTick = originalTick;
        }

        public InjectionConfictException(Conflict conflict)
            :this(conflict.Positions, conflict.Queue, conflict.Tick, conflict.OriginalTick)
        {
        }
    }
}