using System.Linq;
using Robust.Shared.Localization;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    /// <summary>
    ///     Returns a command's localized description.
    /// </summary>
    public string Description(string? subCommand)
        => Loc.GetString(UnlocalizedDescription(subCommand));

    /// <summary>
    ///     Returns the locale string for a command's description.
    /// </summary>
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

    /// <summary>
    ///     Returns a command's help string.
    /// </summary>
    public string GetHelp(string? subCommand)
    {
        if (subCommand is null)
            return $"{Name}: {Description(null)}";
        else
            return $"{Name}:{subCommand}: {Description(subCommand)}";
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetHelp(null);
    }
}
