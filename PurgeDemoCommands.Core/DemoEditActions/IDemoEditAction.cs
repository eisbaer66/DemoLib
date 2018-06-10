using System.IO;
using System.Threading.Tasks;

namespace PurgeDemoCommands.Core.DemoEditActions
{
    public interface IDemoEditAction
    {
        Task Execute(FileStream readStream, FileStream writeStream);
    }
}