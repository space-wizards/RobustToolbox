using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class NearbyCommand : ToolshedCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private EntityLookupSystem? _lookup;

    [CommandImplementation]
    public IEnumerable<EntityUid> Nearby(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] float range
        )
    {
        var limit = _cfg.GetCVar(CVars.ToolshedNearbyLimit);
        if (range > limit)
            throw new ArgumentException($"Tried to query too many entities with nearby ({range})! Limit: {limit}. Change the {CVars.ToolshedNearbyLimit.Name} cvar to increase this at your own risk.");

        _lookup ??= GetSys<EntityLookupSystem>();
        return input.SelectMany(x => _lookup.GetEntitiesInRange(x, range)).Distinct();
    }
}
