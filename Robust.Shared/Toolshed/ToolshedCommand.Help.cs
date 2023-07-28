using System.Linq;
using Robust.Shared.Localization;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    public string Description(string? subCommand)
        => Loc.GetString(UnlocalizedDescription(subCommand));

    public string UnlocalizedDescription(string? subCommand)
    {
        if (Name.All(char.IsAsciiLetterOrDigit))
        {
            return $"command-description-{Name}" + (subCommand is not null ? $"-{subCommand}" : "");
        }
        else
        {
            return $"command-description-{GetType().PrettyName()}" + (subCommand is not null ? $"-{subCommand}" : "");
        }
    }

    public string GetHelp(string? subCommand)
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
