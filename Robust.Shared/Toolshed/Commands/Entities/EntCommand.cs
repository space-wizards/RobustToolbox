using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class EntCommand : ToolshedCommand
{
    [CommandImplementation]
    public EntityUid Ent([CommandArgument] EntityUid ent) => ent;
}

