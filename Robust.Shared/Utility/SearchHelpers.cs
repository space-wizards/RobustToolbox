using System.Globalization;

namespace Robust.Shared.Utility;

/// <summary>
///     Helpers for user-facing text search that use Unicode-aware comparison, so that characters
///     differing only by diacritical marks are treated as equivalent.
/// </summary>
/// <remarks>
///     Uses <see cref="CompareOptions.IgnoreNonSpace"/> via <see cref="CompareInfo"/>, which
///     operates on Unicode NFD decomposition. This means any letter that is canonically a base
///     letter plus a combining mark (diaeresis, acute, grave, tilde, ring, …) will match the
///     bare base letter. Examples:
///     <list type="bullet">
///       <item>Russian ё (е + combining diaeresis) matches е, and vice-versa.</item>
///       <item>German ü / ö / ä match u / o / a.</item>
///       <item>Scandinavian å matches a; ø is not a composed form and is unaffected.</item>
///       <item>French é / è / ê / ë all match e; à / â match a; etc.</item>
///       <item>Spanish ñ matches n.</item>
///     </list>
///     No configuration or setup is required. Any UI code — engine or game content — can call
///     <see cref="ContainsSearch"/> directly.
/// </remarks>
public static class SearchHelpers
{
    private static readonly CompareInfo Comparer = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions SearchOptions = CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase;

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="source"/> contains
    ///     <paramref name="search"/>, ignoring case and diacritical marks.
    /// </summary>
    public static bool ContainsSearch(this string source, string search)
    {
        if (string.IsNullOrEmpty(search)) return true;
        if (string.IsNullOrEmpty(source)) return false;
        return Comparer.IndexOf(source, search, SearchOptions) >= 0;
    }
}
