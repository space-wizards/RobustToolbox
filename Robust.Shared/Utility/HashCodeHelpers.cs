using System;

namespace Robust.Shared.Utility;

/// <summary>
/// Utility functions for working with <see cref="HashCode"/>.
/// </summary>
public static class HashCodeHelpers
{
    /// <summary>
    /// Add the contents of an array to a <see cref="HashCode"/>.
    /// </summary>
    public static void AddArray<T>(ref this HashCode hc, T[]? array)
    {
        if (array == null)
        {
            hc.Add(0);
            return;
        }

        hc.Add(array.Length);

        foreach (var item in array)
        {
            hc.Add(item);
        }
    }
}
