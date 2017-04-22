using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core
{
    public interface IThrottler
    {
        Task<IEnumerable<Result>> Throttle<T>(Func<T, Task<Result>> func, IEnumerable<T> items);
    }
}