using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell.Commands.Entities;

[ConsoleCommand]
internal sealed class PausedCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Paused([PipedArgument] IEnumerable<EntityUid> entities, [CommandInverted] bool inverted)
    {
        return entities.Where(x => Comp<MetaDataComponent>(x).EntityPaused ^ inverted);
    }
}
