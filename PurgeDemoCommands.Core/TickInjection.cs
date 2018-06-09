using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PurgeDemoCommands.Core
{
    public interface ITickInjection
    {
        void Into(IDictionary<int, List<ReplacementPosition>> positions);
    }

    public class TickInjection : ITickInjection
    {
        private readonly int _tick;
        private readonly IDictionary<int, List<string>> _commandsByLength;
        private readonly IConfilctResolver _confilctResolver;
        private readonly ITickIterator _tickIterator;
        private int _minLength;

        public TickInjection(int tick, IEnumerable<string> commands, IConfilctResolver confilctResolver, ITickIterator tickIterator)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));

            _tick = tick;
            _confilctResolver = confilctResolver ?? throw new ArgumentNullException(nameof(confilctResolver));
            _tickIterator = tickIterator ?? throw new ArgumentNullException(nameof(tickIterator));
            _commandsByLength = commands.GroupBy(c => c.Length).ToDictionary(c => c.Key, c => c.ToList());
            CalculateMinLength();
        }

        private void CalculateMinLength()
        {
            if (_commandsByLength.Count == 0)
                _minLength = Int32.MaxValue;
            else
                _minLength = _commandsByLength.Keys.Min();
        }

        public void Into(IDictionary<int, List<ReplacementPosition>> positions)
        {
            int tick = _tick;
            int minTick = positions.Keys.Min();
            int maxTick = positions.Keys.Max();

            while (_commandsByLength.Count > 0 && tick >= minTick && tick <= maxTick)
            {
                if (positions.ContainsKey(tick))
                    Replace(positions, tick);

                tick = _tickIterator.Move(tick);
            }

            if (_commandsByLength.Count > 0)
                throw new NotEnoughCommandsToReplaceException(_tick, _commandsByLength);
        }

        private void Replace(IDictionary<int, List<ReplacementPosition>> positions, int tick)
        {
            List<ReplacementPosition> tickPositions = positions[tick];

            if (!tickPositions.Any(p => p.IsFree))
            {
                _confilctResolver.Resolve(tickPositions, _commandsByLength, tick, _tick);
            }

            foreach (ReplacementPosition position in tickPositions)
            {
                int length = position.Bytes.Length;
                string command = FindCommandWithMaxLength(length);
                if (string.IsNullOrEmpty(command))
                    continue;


                byte[] bytes = Encoding.ASCII.GetBytes(command);
                bytes = FillArray(position, bytes);
                position.Bytes = bytes;
                position.IsFree = false;


                List<string> list = _commandsByLength[command.Length];
                list.Remove(command);
                if (list.Count == 0)
                    _commandsByLength.Remove(command.Length);

                CalculateMinLength();
            }
        }

        private static byte[] FillArray(ReplacementPosition position, byte[] bs)
        {
            byte[] bytes = Enumerable.Range(0, position.Bytes.Length).Select(i => (byte) 0).ToArray();
            for (int i = 0; i < bs.Length; i++)
            {
                bytes[i] = bs[i];
            }

            return bytes;
        }

        private string FindCommandWithMaxLength(int length)
        {
            if (_minLength > length)
                return null;

            while (length >= _minLength)
            {
                if (_commandsByLength.ContainsKey(length))
                    return _commandsByLength[length].FirstOrDefault();
                length--;
            }

            return null;
        }
    }
}