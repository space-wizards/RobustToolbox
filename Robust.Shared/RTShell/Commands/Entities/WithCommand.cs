using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.RTShell.TypeParsers;

namespace Robust.Shared.RTShell.Commands.Entities;

[ConsoleCommand]
internal sealed class WithCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> With([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] ComponentType ty, [CommandInverted] bool inverted)
    {
        return input.Where(x => EntityManager.HasComponent(x, ty.Ty) ^ inverted);
    }
}
