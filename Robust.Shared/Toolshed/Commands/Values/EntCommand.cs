using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
internal sealed class EntCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid Ent(EntityUid ent)
        => ent;
}
