using System;

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

    /// <summary>
    /// Helper method for generating auto-completion hints while parsing command arguments.
    /// </summary>
    public static string GetArgHint(CommandArgument? arg, Type t)
    {
        if (arg == null)
            return t.PrettyName();

        return GetArgHint(arg.Value.Name, arg.Value.IsOptional, arg.Value.IsParamsCollection, t);
    }

    /// <summary>
    /// Helper method for generating auto-completion hints while parsing command arguments.
    /// </summary>
    public static string GetArgHint(string name, bool optional, bool isParams, Type t)
    {
        var type = t.PrettyName();

        // optional arguments wrapped in square braces, inspired by the syntax of man pages
        if (optional)
            return $"[{name} ({type})]";

        // ellipses for params / variable length arguments
        if (isParams)
            return $"[{name} ({type})]...";

        return $"<{name} ({type})>";
    }
}
