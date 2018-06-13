using System.Collections.Generic;
using PurgeDemoCommands.Core.DemoEditActions;

namespace PurgeDemoCommands.Core.CommandInjections
{
    public interface ICommandInjection
    {
        IEnumerable<IDemoEditAction> PlanReplacements(CommandPositions positions);
    }
}