using System.Linq;
using System.Text;

namespace Robust.Shared.Utility;

public static class StringExtensions
{
    /// <summary>
    /// Convert a CamelCase string to snake_case
    /// </summary>
    public static string ToSnakeCase(this string str)
    {
        // If the string is all uppercase, we assume its an acronym. I.e., NPC or HTN.
        if (str.All(char.IsUpper))
            return str.ToLowerInvariant();

        var builder = new StringBuilder();
        foreach (var c in str)
        {
            if (char.IsLower(c))
            {
                builder.Append(c);
            }
            else
            {
                if (builder.Length > 0)
                    builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
