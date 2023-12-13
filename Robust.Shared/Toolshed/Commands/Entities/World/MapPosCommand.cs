using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.Toolshed.Commands.Entities.World;

[ToolshedCommand]
internal sealed class MapPosCommand : ToolshedCommand
{
    private SharedTransformSystem? _xform;

    [CommandImplementation]
    public EntityCoordinates MapPos([PipedArgument] EntityUid ent)
    {
        _xform = GetSys<SharedTransformSystem>();
        var xform = Transform(ent);
        var worldPos = _xform.GetWorldPosition(xform);

        return new EntityCoordinates(xform.MapUid ?? EntityUid.Invalid, worldPos);
    }

    [CommandImplementation]
    public IEnumerable<EntityCoordinates> MapPos([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(MapPos);
}
