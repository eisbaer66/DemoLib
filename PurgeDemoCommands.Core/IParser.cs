using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core
{
    public interface IParser
    {
        Task<IList<CommandPosition>> ReadDemo(string filename);
    }
}