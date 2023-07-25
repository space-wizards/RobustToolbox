using Robust.Shared.Localization;

namespace Robust.Shared.RTShell;

public abstract partial class ConsoleCommand
{
    public string Description(string? subCommand)
        => Loc.GetString($"command-description-{Name}" + (subCommand is not null ? $"-{subCommand}" : ""));

    public string GetHelp(string? subCommand, bool includeName = true)
    {
        if (subCommand is null)
            return $"{Name}: {Description(null)}";
        else
            return $"{Name}:{subCommand}: {Description(subCommand)}";
    }

    public override string ToString()
    {
        return GetHelp(null);
    }
}
