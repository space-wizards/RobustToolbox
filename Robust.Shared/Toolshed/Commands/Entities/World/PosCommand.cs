using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.Toolshed.Commands.Entities.World;

[ToolshedCommand]
internal sealed class PosCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityCoordinates Pos([PipedArgument] EntityUid ent) => Transform(ent).Coordinates;

    [CommandImplementation]
    public IEnumerable<EntityCoordinates> Pos([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Pos);

    [CommandImplementation]
    public EntityCoordinates Pos(IInvocationContext ctx)
    {
        if (ExecutingEntity(ctx) is { } ent)
            return Transform(ent).Coordinates;
        return EntityCoordinates.Invalid;
    }
}
