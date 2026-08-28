using System.Globalization;

namespace Robust.Shared.Utility;

/// <summary>
///     Helpers for user-facing text search that use Unicode-aware comparison, so that characters
///     differing only by diacritical marks are treated as equivalent.
/// </summary>
/// <remarks>
///     Uses <see cref="CompareOptions.IgnoreNonSpace"/>, <see cref="CompareOptions.IgnoreWidth"/>,
///     and <see cref="CompareOptions.IgnoreKanaType"/> via <see cref="CompareInfo"/>. This means:
///     <list type="bullet">
///       <item>Characters that differ only by diacritical marks are equivalent: Russian ё matches е,
///       German ü matches u, French é/è/ê all match e, Spanish ñ matches n, etc.</item>
///       <item>Full-width and half-width variants of the same character are equivalent (relevant for
///       Japanese and Chinese input).</item>
///       <item>Hiragana and Katakana representations of the same sound are equivalent.</item>
///     </list>
///     Uses the current culture so that locale-specific comparison rules are respected.
///     No configuration or setup is required; any UI code can call <see cref="ContainsSearch"/> directly.
/// </remarks>
public static class SearchHelpers
{
    private const CompareOptions SearchOptions =
        CompareOptions.IgnoreCase |
        CompareOptions.IgnoreNonSpace |
        CompareOptions.IgnoreWidth |
        CompareOptions.IgnoreKanaType;

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="source"/> contains
    ///     <paramref name="search"/>, ignoring case, diacritical marks, character width, and
    ///     kana type.
    /// </summary>
    public static bool ContainsSearch(this string source, string search)
    {
        if (string.IsNullOrEmpty(search)) return true;
        if (string.IsNullOrEmpty(source)) return false;
        return CultureInfo.CurrentCulture.CompareInfo.IndexOf(source, search, SearchOptions) >= 0;
    }
}
