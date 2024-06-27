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
        // Description
        var description = subCommand is null
            ? $"{Name}: {Description(null)}"
            : $"{Name}:{subCommand}: {Description(subCommand)}";

        // Usage
        var usage = new StringBuilder();
        usage.Append("Usage: ");
        foreach (var (pipedType, parameters) in ReadonlyParameters[subCommand ?? ""])
        {
            usage.AppendLine();

            // Piped type
            if (pipedType != null)
                // arguments.Append(Loc.GetString("piped-type-string", ("pipedType", pipedType.ToString())));
                usage.Append($"(PipedType: {pipedType.ToString()})");

            // Name
            usage.Append(Name);

            // Parameters
            foreach (var param in parameters)
            {
                // arguments.Append($"{Loc.GetString($"command-description-{Name}-param-{param.Key}")}({param.Value}");
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
