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
            [CommandArgument(typeof(ComponentTypeParser))] Type ty,
            [CommandInverted] bool inverted
        )
    {
        if (inverted)
            return input.Where(x => !EntityManager.HasComponent(x, ty));

        // Special case when iterating over **all** entities.
        if (input is EntitiesCommand.AllEntityEnumerator)
            return Query(ty).ToArray();

        return input.Where(x => EntityManager.HasComponent(x, ty));
    }

    private IEnumerable<EntityUid> Query(Type ty)
    {
        if (!ty.IsAssignableTo(typeof(IComponent)))
            throw new ArgumentException("Type is not a component");

        var query = EntityManager.AllEntityQueryEnumerator(ty);
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation]
    public IEnumerable<EntityPrototype> With(
        [PipedArgument] IEnumerable<EntityPrototype> input,
        [CommandArgument(typeof(ComponentTypeParser))] Type ty,
        [CommandInverted] bool inverted
    )
    {
        var name = _componentFactory.GetComponentName(ty);
        return input.Where(x => x.Components.ContainsKey(name) ^ inverted);
    }
}
