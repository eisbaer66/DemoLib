using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core
{
    public interface IPurgeCommand
    {
        IList<string> Filenames { get; set; }
        string NewFilePattern { get; set; }
        bool Overwrite { get; set; }
        IFilter Filter { get; set; }
        void SetCommands(string[] value);
        Task<IEnumerable<Result>> ExecuteThrottled();
        IEnumerable<Task<Result>> Execute();
    }
}