using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility;

/// <summary>
///     Helpers for user-facing text search that fold interchangeable characters together, so that a
///     search finds results regardless of which variant the player happened to type.
/// </summary>
/// <remarks>
///     <para>
///     Some languages have letters that players treat as interchangeable when typing. For example, in
///     Russian the letters 'е' and 'ё' are routinely used interchangeably, so searching for one should
///     also match the other.
///     </para>
///     <para>
///     Any UI search, including UI written in game content, can simply call <see cref="ContainsSearch"/>
///     (or <see cref="NormalizeForSearch"/>); there is no setup required. The set of equivalent
///     characters is read from the <c>interface.search_char_equivalences</c> CVar
///     (see <see cref="CVars.SearchCharEquivalences"/>), so games or localizations can adjust it for
///     other languages without code changes.
///     </para>
/// </remarks>
public static class SearchHelpers
{
    private static readonly object InitLock = new();

    // Maps a character to the canonical character it is folded to when normalizing text for search.
    // Replaced wholesale (atomic reference assignment) whenever the configured value changes.
    private static IReadOnlyDictionary<char, char> _equivalences = new Dictionary<char, char>();
    private static bool _subscribed;

    /// <summary>
    ///     Determines whether <paramref name="source"/> contains <paramref name="search"/>, folding
    ///     any configured interchangeable characters together before comparing. The comparison is
    ///     case-insensitive using the current culture.
    /// </summary>
    public static bool ContainsSearch(this string source, string search)
    {
        return source.ContainsSearch(search, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    ///     Determines whether <paramref name="source"/> contains <paramref name="search"/>, folding
    ///     any configured interchangeable characters together before comparing.
    /// </summary>
    /// <param name="source">The text being searched.</param>
    /// <param name="search">The text to look for.</param>
    /// <param name="comparison">The string comparison rules to use for the rest of the comparison.</param>
    public static bool ContainsSearch(this string source, string search, StringComparison comparison)
    {
        return NormalizeForSearch(source).Contains(NormalizeForSearch(search), comparison);
    }

    /// <summary>
    ///     Folds any configured interchangeable characters in <paramref name="value"/> to their
    ///     canonical form. Returns the original string instance when nothing needs changing.
    /// </summary>
    public static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        EnsureLoaded();

        var table = _equivalences;
        if (table.Count == 0)
            return value;

        char[]? buffer = null;
        for (var i = 0; i < value.Length; i++)
        {
            if (!table.TryGetValue(value[i], out var replacement))
                continue;

            buffer ??= value.ToCharArray();
            buffer[i] = replacement;
        }

        return buffer == null ? value : new string(buffer);
    }

    // Lazily binds the equivalence table to configuration the first time a search runs on a thread
    // that has access to the configuration manager. Until then, no folding is applied.
    private static void EnsureLoaded()
    {
        if (_subscribed)
            return;

        lock (InitLock)
        {
            if (_subscribed)
                return;

            // Config may not be registered yet (e.g. very early startup or headless contexts).
            // Leave it unsubscribed so a later call can try again.
            if (IoCManager.Instance is not { } deps || !deps.TryResolveType<IConfigurationManager>(out var cfg))
                return;

            cfg.OnValueChanged(CVars.SearchCharEquivalences, ParseEquivalences, invokeImmediately: true);
            _subscribed = true;
        }
    }

    // Parses the comma-separated "from/to" pairs from the CVar into a fresh lookup table.
    private static void ParseEquivalences(string raw)
    {
        var table = new Dictionary<char, char>();

        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Each pair must be exactly two characters: the typed variant and the canonical form.
            if (pair.Length != 2)
                continue;

            var from = pair[0];
            var to = pair[1];

            // Register both case variants automatically, so a single "ёе" pair also covers "ЁЕ".
            // Folding happens before the case-insensitive compare, so an unfolded uppercase variant
            // would otherwise still fail to match.
            table[char.ToLowerInvariant(from)] = char.ToLowerInvariant(to);
            table[char.ToUpperInvariant(from)] = char.ToUpperInvariant(to);
        }

        _equivalences = table;
    }
}
