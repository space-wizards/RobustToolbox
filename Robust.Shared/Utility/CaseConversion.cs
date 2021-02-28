using System.Text.RegularExpressions;

namespace Robust.Shared.Utility
{
    public static class CaseConversion
    {
        private static readonly Regex PascalToKebabRegex =
            new Regex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", RegexOptions.Compiled);

        public static string PascalToKebab(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            return PascalToKebabRegex.Replace(str, "-$1").Trim().ToLower();
        }
    }
}
