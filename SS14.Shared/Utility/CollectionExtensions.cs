using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.Utility
{
    public static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T) item.Clone()).ToList();
        }

        // TODO: verify whether C#'s List<T> implementation
        //         won't explode and cause performance issues.
        /// <summary>
        /// Remove an item from the list, replacing it with the one at the very end of the list.
        /// This means that the order will not be preserved, but it should be an O(1) operation.
        /// </summary>
        /// <param name="index">The index to remove</param>
        /// <returns>The removed element</returns>
        public static T RemoveSwap<T>(this IList<T> list, int index)
        {
            T old = list[index];
            T replacement = list[list.Count-1];
            list[index] = replacement;
            // TODO: Any more efficient way to pop the last element off?
            list.RemoveAt(list.Count-1);
            return old;
        }
    }
}
