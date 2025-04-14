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
    public IEnumerable<EntityUid> Nearby([PipedArgument] IEnumerable<EntityUid> input, float range)
    {
        var rangeLimit = _cfg.GetCVar(CVars.ToolshedNearbyLimit);
        if (range > rangeLimit)
            throw new ArgumentException($"Tried to query too big of a range with nearby ({range})! Limit: {rangeLimit}. Change the {CVars.ToolshedNearbyLimit.Name} cvar to increase this at your own risk.");

        _lookup ??= GetSys<EntityLookupSystem>();

        var i = 0;
        var entitiesLimit = _cfg.GetCVar(CVars.ToolshedNearbyEntitiesLimit);
        return input.SelectMany(x =>
            {
                if (i++ > entitiesLimit)
                {
                    throw new ArgumentException(
                        $"Too many entities were passed to nearby ({i})! Limit: {entitiesLimit}. Change the {CVars.ToolshedNearbyEntitiesLimit.Name} cvar to increase this at your own risk.");
                }

                return _lookup.GetEntitiesInRange(x, range);
            })
            .Distinct();
    }
}
