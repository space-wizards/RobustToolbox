using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Robust.Shared.Utility;

public static class CultureInfoHelper
{
    public static readonly FrozenSet<string> CultureNames = GenerateAllCultureNames();

    /// <exception cref="InvalidCastException"></exception>
    public static CultureInfo GetCultureInfo(string code)
    {
        if (!HasCulture(code))
            throw new InvalidCastException();

        return new CultureInfo(code);
    }

    /// <exception cref="InvalidCastException"></exception>
    public static void GetCultureInfo(string code, out CultureInfo culture)
    {
        culture = GetCultureInfo(code);
    }

    public static bool TryGetCultureInfo(string code, [NotNullWhen(true)] out CultureInfo? culture)
    {
        culture = null;
        if (!HasCulture(code))
            return false;

        culture = new CultureInfo(code);
        return true;
    }

    public static bool HasCulture(string code)
    {
        return CultureNames.Contains(code);
    }

    /// <summary>
    /// Creates all possible Cultures as a <see cref="FrozenSet{T}"/> for fast operation.
    /// </summary>
    /// <summary>
    /// Take from <a href="https://stackoverflow.com/a/36560072">stackoverflow</a>
    /// </summary>
    private static FrozenSet<string> GenerateAllCultureNames()
    {
        var cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .ToArray();

        var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allNames.UnionWith(cultureInfos.Select(x => x.TwoLetterISOLanguageName));
        allNames.UnionWith(cultureInfos.Select(x => x.Name));

        return allNames.ToFrozenSet();
    }
}
