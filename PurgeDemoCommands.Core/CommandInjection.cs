using System.Collections.Generic;
using System.Linq;

namespace PurgeDemoCommands.Core
{
    public class CommandInjection : ICommandInjection
    {
        private IEnumerable<ITickInjection> _tickInjections;

        public CommandInjection(IEnumerable<ITickInjection> tickInjections)
        {
            _tickInjections = tickInjections;
        }

        public IList<ReplacementPosition> PlanReplacements(IList<CommandPosition> positions)
        {
            IDictionary<int, List<ReplacementPosition>> dict = positions.GroupBy(p => p.Tick, p => ReplacementPosition.Nulls(p.Index, p.NumberOfBytes)).ToDictionary(g => g.Key, g => g.ToList());

            foreach (ITickInjection tickInjection in _tickInjections)
            {
                tickInjection.Into(dict);
            }

            return dict.Values.SelectMany(v => v).ToList();
        }
    }
}