using System;
using System.Collections.Immutable;

namespace Robust.Generators;

public sealed class ModelHelper
{
    public static bool ArrayEquals<T>(ImmutableArray<T> a, ImmutableArray<T> b) where T : IEquatable<T>
    {
        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++)
        {
            var iA = a[i];
            var iB = b[i];

            if (!iA.Equals(iB))
                return false;
        }

        return true;
    }

    public static int ArrayHashCode<T>(ImmutableArray<T> array) where T : IEquatable<T>
    {
        var hashCode = 0;

        foreach (var item in array)
        {
            hashCode = (hashCode * 397) ^ item.GetHashCode();
        }

        return hashCode;
    }
}
