using System.Collections.Generic;
using System.Linq;
using PurgeDemoCommands.Core.DemoEditActions;

namespace PurgeDemoCommands.Core.CommandInjections
{
    public class CommandInjection : ICommandInjection
    {
        private IEnumerable<ITickInjection> _tickInjections;
        private IBuffer _buffer;

        public CommandInjection(IEnumerable<ITickInjection> tickInjections)
        {
            _tickInjections = tickInjections;
            _buffer = new Buffer();
        }

        public IEnumerable<IDemoEditAction> PlanReplacements(CommandPositions positions)
        {
            Queue<ITickInjection> queue = new Queue<ITickInjection>(_tickInjections.OrderBy(i => i.Tick));
            ITickInjection nextTickInjection = null;
            if (queue.Count > 0)
                nextTickInjection = queue.Dequeue();

            bool isFirst = true;
            foreach (CommandPosition position in positions.Positions)
            {
                if (isFirst)
                {
                    yield return new CopyDemoEditAction(_buffer)
                    {
                        Index = 0,
                        Length = (int)position.Index + position.NumberOfBytes,
                        Tick = position.Tick
                    };
                    isFirst = false;
                    continue;
                }

                while (nextTickInjection != null && position.Tick > nextTickInjection.Tick && position.Index >= positions.MinimumIndex)
                {
                    yield return new InsertDemoEditAction()
                    {
                        Injection = nextTickInjection,
                    };

                    if (queue.Count > 0)
                        nextTickInjection = queue.Dequeue();
                    else
                        nextTickInjection = null;
                }

                if (position.IsConsoleCommand)
                    continue;

                yield return new CopyDemoEditAction(_buffer)
                {
                    Index = position.Index,
                    Length = position.NumberOfBytes,
                    Tick = position.Tick
                };
            }

            yield return new CopyFileRemainderDemoEditAction(_buffer);
        }
    }
}