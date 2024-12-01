using System;
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
            [CommandArgument(typeof(ComponentTypeParser))] Type component,
            [CommandInverted] bool inverted
        )
    {
        if (inverted)
            return input.Where(x => !EntityManager.HasComponent(x, component));

        if (input is EntitiesCommand.AllEntityEnumerator)
            return EntityManager.AllEntityUids(component);

        return input.Where(x => EntityManager.HasComponent(x, component));
    }

    [CommandImplementation]
    public IEnumerable<EntityPrototype> With(
        [PipedArgument] IEnumerable<EntityPrototype> input,
        [CommandArgument(typeof(ComponentTypeParser))] Type component,
        [CommandInverted] bool inverted
    )
    {
        var name = _componentFactory.GetComponentName(component);
        return input.Where(x => x.Components.ContainsKey(name) ^ inverted);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<ProtoId<T>> With<T>(
        [PipedArgument] IEnumerable<ProtoId<T>> input,
        ProtoId<T> protoId,
        [CommandInverted] bool inverted
    ) where T : class, IPrototype
    {
        return input.Where(x => (x == protoId) ^ inverted);
    }
}
