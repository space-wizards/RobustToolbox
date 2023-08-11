using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Shared.Toolshed.Commands.Entities.World;

[ToolshedCommand]
public sealed class PosCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityCoordinates Pos([PipedArgument] EntityUid ent) => Transform(ent).Coordinates;
}
