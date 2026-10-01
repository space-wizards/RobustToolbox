using System.Collections.Generic;

namespace Robust.Shared.Utility;

public static class CollectionHelpers
{
    public static bool ContainsDuplicates<T>(IReadOnlyList<T> list)
    {
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                if (comparer.Equals(list[i], list[j]))
                    return true;
            }
        }

        return false;
    }
}
