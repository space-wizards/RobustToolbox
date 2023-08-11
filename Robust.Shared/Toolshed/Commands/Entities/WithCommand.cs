using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class WithCommand : ToolshedCommand
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> With(
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] ComponentType ty,
            [CommandInverted] bool inverted
        )
    {
        return input.Where(x => EntityManager.HasComponent(x, ty.Ty) ^ inverted);
    }

    [CommandImplementation]
    public IEnumerable<EntityPrototype> With(
        [PipedArgument] IEnumerable<EntityPrototype> input,
        [CommandArgument] ComponentType ty,
        [CommandInverted] bool inverted
    )
    {
        var name = _componentFactory.GetComponentName(ty.Ty);
        return input.Where(x => x.Components.ContainsKey(name) ^ inverted);
    }
}
