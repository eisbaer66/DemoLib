using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core
{
    public interface IParser
    {
        Task<CommandPositions> ReadDemo(string filename);
    }
}