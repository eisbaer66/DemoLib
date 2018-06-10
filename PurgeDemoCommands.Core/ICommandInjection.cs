using System.Collections.Generic;
using PurgeDemoCommands.Core.DemoEditActions;

namespace PurgeDemoCommands.Core
{
    public interface ICommandInjection
    {
        IEnumerable<IDemoEditAction> PlanReplacements(CommandPositions positions);
    }
}