using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell.Commands.Entities;

[ConsoleCommand]
internal sealed class EntCommand : ConsoleCommand
{
    [CommandImplementation]
    public EntityUid Ent([CommandArgument] EntityUid ent) => ent;
}

