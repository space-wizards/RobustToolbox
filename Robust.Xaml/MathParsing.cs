using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Robust.Xaml;

internal static class MathParsing
{
    private static float[]? ParseSingleArr(string input)
    {
        // Transliteration note: The original patterns in this file were Pidgin parsers
        // All of them were variations on Real.Select(c => (float c)).Between(SkipWhiteSpaces).Repeat(n)
        // They somehow handled commas too, but I don't know how
        //
        // SkipWhitespace splits based on char.IsWhitespace:
        // https://github.com/benjamin-hodgson/Pidgin/blob/cc72abb/Pidgin/Parser.Whitespace.cs#L30
        var items = SplitStringByFunction(input, (c) => c == ',' || char.IsWhiteSpace(c));
        var outs = new float[items.Count];

        for (var i = 0; i < outs.Length; i++)
        {
            // Parser.Real ultimately resorts to double.TryParse
            // https://github.com/benjamin-hodgson/Pidgin/blob/cc72abb/Pidgin/Parser.Number.cs#L222
            var parsed = double.TryParse(
                items[i],
                NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var d
            );
            if (!parsed)
            {
                return null;
            }
            outs[i] = (float)d;
        }

        return outs;
    }

    private static List<string> SplitStringByFunction(string s, Func<char, bool> isSeparator)
    {
        // we want to split by commas _or_ char.IsWhitespace
        // C#'s Split() can do one but not both
        var splitItems = new List<string>();
        var itemInProgress = new StringBuilder();
        foreach (var c in s)
        {
            if (isSeparator(c))
            {
                if (itemInProgress.Length > 0)
                {
                    splitItems.Add(itemInProgress.ToString());
                    itemInProgress.Clear();
                }
            }
            else
            {
                itemInProgress.Append(c);
            }
        }

        if (itemInProgress.Length > 0)
        {
            splitItems.Add(itemInProgress.ToString());
        }

        return splitItems;
    }

    /// <summary>
    /// Parse a vector of two floats separated by commas or spaces, such as
    /// "1,2" or "1.5 2.5"
    /// </summary>
    /// <param name="s">the string representation of the vector</param>
    /// <returns>the parsed floats, or null if parsing failed</returns>
    public static (float, float)? ParseVector2(string s)
    {
        var arr = ParseSingleArr(s);
        if (arr == null)
        {
            return null;
        }
        if (arr.Length == 2)
        {
            return (arr[0], arr[1]);
        }

        return null;
    }

    /// <summary>
    /// Parse a vector of one, two, or four floats separated by commas or
    /// spaces, such as "1", "1e2,2e3" or ".1,.2,.3,.4"
    /// </summary>
    /// <param name="s">the string representation of the vector</param>
    /// <returns>the parsed floats, or null if parsing failed</returns>
    public static float[]? ParseThickness(string s)
    {
        var arr = ParseSingleArr(s);
        if (arr == null)
        {
            return null;
        }
        if (arr.Length == 1 || arr.Length == 2 || arr.Length == 4)
        {
            return arr;
        }

        return null;
    }
}
