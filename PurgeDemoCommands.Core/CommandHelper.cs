using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PurgeDemoCommands.Core
{
    public class CommandHelper : IThrottler
    {
        public async Task<IEnumerable<Result>> Throttle<T>(Func<T, Task<Result>> func, IEnumerable<T> items)
        {
            TransformBlock<T, Result> purges = new TransformBlock<T, Result>(func,
                new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = Environment.ProcessorCount});
            BufferBlock<Result> buffer = new BufferBlock<Result>();
            purges.LinkTo(buffer);

            foreach (T filename in items)
            {
                purges.Post(filename);
            }

            purges.Complete();
            await purges.Completion;

            IList<Result> results;
            if (buffer.TryReceiveAll(out results))
                return results;

            throw new ExecuteException();
        }
    }
}