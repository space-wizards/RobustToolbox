using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.RTShell.TypeParsers;

namespace Robust.Shared.RTShell.Commands.Entities;

[RtShellCommand]
internal sealed class WithCommand : RtShellCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> With([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] ComponentType ty, [CommandInverted] bool inverted)
    {
        return input.Where(x => EntityManager.HasComponent(x, ty.Ty) ^ inverted);
    }
}
