using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Utility;

internal static class SpanSplitExtensions
{
    public static bool SplitFindNext<T>(
        ref ReadOnlySpan<T> source,
        T splitOn,
        out ReadOnlySpan<T> splitValue)
        where T : IEquatable<T>
    {
        if (source.IsEmpty)
        {
            splitValue = ReadOnlySpan<T>.Empty;
            return false;
        }

        var idx = source.IndexOf(splitOn);
        if (idx == -1)
        {
            // Take rest of remaining span.
            splitValue = source;
            source = ReadOnlySpan<T>.Empty;
        }
        else
        {
            splitValue = source[..idx];
            source = source[(idx + 1)..];
        }

        return true;
    }
}
