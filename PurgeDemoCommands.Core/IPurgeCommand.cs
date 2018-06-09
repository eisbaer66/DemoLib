using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core
{
    public interface IThrottledPurgeCommand
    {
        Task<IEnumerable<Result>> Execute();
    }
    public interface IPurgeCommand
    {
        Task<Result> Purge();
    }
}