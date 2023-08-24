using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class NearbyCommand : ToolshedCommand
{
    private EntityLookupSystem? _lookup;

    [CommandImplementation]
    public IEnumerable<EntityUid> Nearby(
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] float range
        )
    {
        _lookup ??= GetSys<EntityLookupSystem>();
        return input.SelectMany(x => _lookup.GetEntitiesInRange(x, range)).Distinct();
    }
}
