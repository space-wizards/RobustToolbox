namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    /// <summary>
    ///     Returns a command's localized description.
    /// </summary>
    public string Description(string? subCommand)
    {
        CommandImplementors.TryGetValue(subCommand ?? string.Empty, out var impl);
        return impl?.Description() ?? string.Empty;
    }

    /// <summary>
    ///     Returns the locale string for a command's description.
    /// </summary>
    public string DescriptionLocKey(string? subCommand)
    {
        CommandImplementors.TryGetValue(subCommand ?? string.Empty, out var impl);
        return impl?.DescriptionLocKey() ?? string.Empty;
    }

    /// <summary>
    ///     Returns a command's help string.
    /// </summary>
    public string GetHelp(string? subCommand)
    {
        CommandImplementors.TryGetValue(subCommand ?? string.Empty, out var impl);
        return impl?.GetHelp() ?? string.Empty;
    }

    public override string ToString()
    {
        return Name;
    }
}
