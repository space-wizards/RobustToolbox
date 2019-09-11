using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Utility
{
    public static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        /// <summary>
        ///     Creates a shallow clone of a list.
        ///     Basically a new list with all the same elements.
        /// </summary>
        /// <param name="self">The list to shallow clone.</param>
        /// <typeparam name="T">The type of the list's elements.</typeparam>
        /// <returns>A new list with the same elements as <paramref name="list" />.</returns>
        public static List<T> ShallowClone<T>(this List<T> self)
        {
            var list = new List<T>(self.Count);
            list.AddRange(self);
            return list;
        }

        public static Dictionary<TKey, TValue> ShallowClone<TKey, TValue>(this Dictionary<TKey, TValue> self)
        {
            var dict = new Dictionary<TKey, TValue>(self.Count);
            foreach (var item in self)
            {
                dict[item.Key] = item.Value;
            }
            return dict;
        }

        /// <summary>
        ///     Remove an item from the list, replacing it with the one at the very end of the list.
        ///     This means that the order will not be preserved, but it should be an O(1) operation.
        /// </summary>
        /// <param name="index">The index to remove</param>
        /// <returns>The removed element</returns>
        public static T RemoveSwap<T>(this IList<T> list, int index)
        {
            // This method has no implementation details,
            // and changing the result of an operation is a breaking change.
            var old = list[index];
            var replacement = list[list.Count - 1];
            list[index] = replacement;
            // TODO: Any more efficient way to pop the last element off?
            list.RemoveAt(list.Count - 1);
            return old;
        }

        // So you can do foreach (var (key, value) in dict)
        // This basically re-implements the CoreFX version because we're using a pretty old version of .NET Framework.
        // https://github.com/dotnet/roslyn/issues/20393
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> k, out TKey t, out TValue u) {
            t = k.Key; u = k.Value;
        }

        /// <summary>
        ///     Pop an element from the end of a list, removing it from the list and returning it.
        /// </summary>
        /// <param name="list">The list to pop from.</param>
        /// <typeparam name="T">The type of the elements of the list.</typeparam>
        /// <returns>The popped off element.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the list is empty.
        /// </exception>
        public static T Pop<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var t = list[list.Count - 1];
            list.RemoveAt(list.Count-1);
            return t;
        }

        /// <summary>
        ///     Just like FirstOrDefault but returns null for value types as well.
        /// </summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <returns> null if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.</exception>
        public static TSource? FirstOrNull<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        where TSource: struct
        {
            if (source == null)
                throw new ArgumentNullException(nameof (source));
            if (predicate == null)
                throw new ArgumentNullException(nameof (predicate));
            foreach (TSource source1 in source)
            {
                if (predicate(source1))
                    return source1;
            }
            return null;
        }
    }
}
