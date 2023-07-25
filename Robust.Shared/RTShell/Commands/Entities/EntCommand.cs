using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell.Commands.Entities;

[RtShellCommand]
internal sealed class EntCommand : RtShellCommand
{
    [CommandImplementation]
    public EntityUid Ent([CommandArgument] EntityUid ent) => ent;
}

