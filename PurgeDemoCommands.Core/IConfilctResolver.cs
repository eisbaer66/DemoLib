using System;
using System.Collections.Generic;

namespace PurgeDemoCommands.Core
{
    public interface IConfilctResolver
    {
        void Resolve(IList<ReplacementPosition> positions, IDictionary<int, List<string>> queue, int tick, int originalTick);
    }

    public class AbortingConfilctResolver : IConfilctResolver
    {
        public IList<Conflict> Confilcts { get; set; } = new List<Conflict>();

        public void Resolve(IList<ReplacementPosition> positions, IDictionary<int, List<string>> queue, int tick, int originalTick)
        {
            Conflict conflict = new Conflict(positions, queue, tick, originalTick);
            Confilcts.Add(conflict);
            throw new InjectionConfictException(conflict);
        }
    }

    public class Conflict
    {
        public Conflict(IList<ReplacementPosition> positions, IDictionary<int, List<string>> queue, int tick, int originalTick)
        {
            if (tick <= 0) throw new ArgumentOutOfRangeException(nameof(tick));
            if (originalTick <= 0) throw new ArgumentOutOfRangeException(nameof(originalTick));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            Tick = tick;
            OriginalTick = originalTick;
        }

        public IList<ReplacementPosition> Positions { get; }

        public IDictionary<int, List<string>> Queue { get; }

        public int Tick { get; }

        public int OriginalTick { get; }
    }
}