using System.Collections.Generic;

namespace PurgeDemoCommands.Core
{
    public interface ICommandInjection
    {
        IList<ReplacementPosition> PlanReplacements(IList<CommandPosition> positions);
    }
}