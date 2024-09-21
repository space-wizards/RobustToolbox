using System;
using System.Linq;
using System.Text;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    /// <summary>
    ///     Returns a command's localized description.
    /// </summary>
    public string Description(string? subCommand)
        => Loc.GetString(DescriptionLocKey(subCommand));

    /// <summary>
    ///     Returns the locale string for a command's description.
    /// </summary>
    public string DescriptionLocKey(string? subCommand)
    {
        return $"command-description-{GetLocKeyName(subCommand)}";
    }

    /// <summary>
    /// Get the full name of a command for use when fetching localized strings.
    /// </summary>
    public string GetLocKeyName(string? subCommand)
    {
        var name = Name.All(char.IsAsciiLetterOrDigit)
            ? Name
            : GetType().PrettyName();

        return subCommand == null
            ? name
            : $"{name}-{subCommand}";
    }

    /// <summary>
    ///     Returns a command's help string.
    /// </summary>
    public string GetHelp(string? subCommand)
    {
        // Description
        var description = subCommand is null
            ? $"{Name}: {Description(null)}"
            : $"{Name}:{subCommand}: {Description(subCommand)}";

        // Usage
        var usage = new StringBuilder();
        usage.AppendLine();
        usage.Append(Loc.GetString("command-description-usage"));
        foreach (var (pipedType, parameters) in _readonlyParameters[subCommand ?? ""])
        {
            usage.Append(Environment.NewLine + "  ");

            // Piped type
            if (pipedType != null)
            {
                usage.Append(Loc.GetString("command-description-usage-pipedtype",
                    ("typeName", GetFriendlyName(pipedType))));
            }

            // Name
            usage.Append(Name);

            // Parameters
            foreach (var param in parameters)
            {
                usage.Append($" <{GetFriendlyName(param)}>");
            }
        }

        return description + usage;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetHelp(null);
    }

    public static string GetFriendlyName(Type type)
    {
        string friendlyName = type.Name;
        if (type.IsGenericType)
        {
            int iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
            {
                friendlyName = friendlyName.Remove(iBacktick);
            }
            friendlyName += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                string typeParamName = GetFriendlyName(typeParameters[i]);
                friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
            }
            friendlyName += ">";
        }

        return friendlyName;
    }
}
