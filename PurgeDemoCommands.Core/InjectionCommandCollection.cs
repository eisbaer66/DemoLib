using System.Collections.Generic;

namespace PurgeDemoCommands.Core
{
    public class InjectionCommandCollection : IInjectionCommandCollection
    {
        private Queue<Queue<string>> _queue;

        public InjectionCommandCollection(Queue<Queue<string>> queue)
        {
            _queue = queue;
        }

        public string GetCommand(long numberOfBytes)
        {
            if (_queue.Count == 0)
                return null;

            string returnValue;
            Queue<string> q = _queue.Peek();

            string command = q.Peek();
            if (command.Length > numberOfBytes)
                return null;

            returnValue = q.Dequeue();

            if (q.Count == 0)
                _queue.Dequeue();

            return returnValue;
        }
    }
}