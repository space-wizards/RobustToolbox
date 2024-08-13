using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

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
        var entitiesLimit = _cfg.GetCVar(CVars.ToolshedNearbyEntitiesLimit);
        if (input.HasMoreThan(entitiesLimit))
            throw new ArgumentException($"Too many entities were passed to nearby ({range})! Limit: {entitiesLimit}. Change the {CVars.ToolshedNearbyEntitiesLimit.Name} cvar to increase this at your own risk.");

        var rangeLimit = _cfg.GetCVar(CVars.ToolshedNearbyLimit);
        if (range > rangeLimit)
            throw new ArgumentException($"Tried to query too big of a range with nearby ({range})! Limit: {rangeLimit}. Change the {CVars.ToolshedNearbyLimit.Name} cvar to increase this at your own risk.");

        _lookup ??= GetSys<EntityLookupSystem>();
        return input.SelectMany(x => _lookup.GetEntitiesInRange(x, range)).Distinct();
    }
}
