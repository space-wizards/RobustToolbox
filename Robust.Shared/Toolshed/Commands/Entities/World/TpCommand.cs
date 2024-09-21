using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.Toolshed.Commands.Entities.World;

[ToolshedCommand]
internal sealed class TpCommand : ToolshedCommand
{
    private SharedTransformSystem? _xform;

    [CommandImplementation("coords")]
    public EntityUid TpCoords([PipedArgument] EntityUid teleporter, EntityCoordinates target)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        _xform.SetCoordinates(teleporter, target);
        return teleporter;
    }

    [CommandImplementation("coords")]
    public IEnumerable<EntityUid> TpCoords([PipedArgument] IEnumerable<EntityUid> teleporters, EntityCoordinates target)
        => teleporters.Select(x => TpCoords(x, target));

    [CommandImplementation("to")]
    public EntityUid TpTo([PipedArgument] EntityUid teleporter, EntityUid target)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        _xform.SetCoordinates(teleporter, Transform(target).Coordinates);
        return teleporter;
    }

    [CommandImplementation("to")]
    public IEnumerable<EntityUid> TpTo([PipedArgument] IEnumerable<EntityUid> teleporters, EntityUid target)
        => teleporters.Select(x => TpTo(x, target));

    [CommandImplementation("into")]
    public EntityUid TpInto([PipedArgument] EntityUid teleporter, EntityUid target)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        _xform.SetCoordinates(teleporter, new EntityCoordinates(target, Vector2.Zero));
        return teleporter;
    }

    [CommandImplementation("into")]
    public IEnumerable<EntityUid> TpInto([PipedArgument] IEnumerable<EntityUid> teleporters, EntityUid target)
        => teleporters.Select(x => TpInto(x, target));
}
