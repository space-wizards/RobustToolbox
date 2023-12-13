using Robust.Shared.Localization;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class LocCommand : ToolshedCommand
{
    [CommandImplementation("tryloc")]
    public string? TryLocalize([PipedArgument] string str)
    {
        Loc.TryGetString(str, out var loc);
        return loc;
    }

    [CommandImplementation("loc")]
    public string Localize([PipedArgument] string str) => Loc.GetString(str);
}
