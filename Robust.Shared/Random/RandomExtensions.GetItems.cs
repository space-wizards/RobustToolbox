using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Collections;

namespace Robust.Shared.Random;

public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary>
        ///     Get set amount of random items from a collection.
        ///     If <paramref name="allowDuplicates"/> is false and <paramref name="source"/>
        ///     is smaller then <paramref name="count"/> - returns shuffled <paramref name="source"/> clone.
        ///     If <paramref name="source"/> is empty, and/or <paramref name="count"/> is 0, returns empty.
        /// </summary>
        /// <param name="source">Collection from which items should be picked.</param>
        /// <param name="count">Number of random items to be picked.</param>
        /// <param name="allowDuplicates">If true, items are allowed to be picked more than once.</param>
        [MustUseReturnValue]
        public TItem[] GetItems<TItem>(IList<TItem> source, int count, bool allowDuplicates = true)
        {
            if (source.Count == 0 || count <= 0)
                return Array.Empty<TItem>();

            if (allowDuplicates == false && count >= source.Count)
            {
                var arr = source.ToArray();
                // Explicit type cast to IList<T> to avoid calling the Span<T> overload.
                // We have some tests that rely on mocking of this call, and Moq doesn't support Span<T> atm.
                // https://github.com/space-wizards/RobustToolbox/issues/6329
                random.Shuffle((IList<TItem>)arr);
                return arr;
            }

            var sourceCount = source.Count;
            var result = new TItem[count];

            if (allowDuplicates)
            {
                for (var i = 0; i < count; i++)
                {
                    result[i] = source[random.Next(sourceCount)];
                }

                return result;
            }

            var indices = sourceCount <= 1024 ? stackalloc int[sourceCount] : new int[sourceCount];
            for (var i = 0; i < sourceCount; i++)
            {
                indices[i] = i;
            }

            for (var i = 0; i < count; i++)
            {
                var j = random.Next(sourceCount - i);
                result[i] = source[indices[j]];
                indices[j] = indices[sourceCount - i - 1];
            }

            return result;
        }

        /// <inheritdoc cref="GetItems{T}(System.Collections.Generic.IList{T},int,bool)"/>
        [MustUseReturnValue]
        public TItem[] GetItems<TItem>(ValueList<TItem> source, int count, bool allowDuplicates = true)
        {
            return random.GetItems(source.Span, count, allowDuplicates);
        }

        /// <inheritdoc cref="GetItems{T}(System.Collections.Generic.IList{T},int,bool)"/>
        [MustUseReturnValue]
        public TItem[] GetItems<TItem>(TItem[] source, int count, bool allowDuplicates = true)
        {
            return random.GetItems(source.AsSpan(), count, allowDuplicates);
        }

        /// <inheritdoc cref="GetItems{T}(System.Collections.Generic.IList{T},int,bool)"/>
        [MustUseReturnValue]
        public TItem[] GetItems<TItem>(Span<TItem> source, int count, bool allowDuplicates = true)
        {
            if (source.Length == 0 || count <= 0)
                return Array.Empty<TItem>();

            if (allowDuplicates == false && count >= source.Length)
            {
                var arr = source.ToArray();
                // Explicit type cast to IList<T> to avoid calling the Span<T> overload.
                // We have some tests that rely on mocking of this call, and Moq doesn't support Span<T> atm.
                // https://github.com/space-wizards/RobustToolbox/issues/6329
                random.Shuffle((IList<TItem>)arr);
                return arr;
            }

            var sourceCount = source.Length;
            var result = new TItem[count];

            if (allowDuplicates)
            {
                for (var i = 0; i < count; i++)
                {
                    result[i] = source[random.Next(sourceCount)];
                }

                return result;
            }

            var indices = sourceCount <= 1024 ? stackalloc int[sourceCount] : new int[sourceCount];
            for (var i = 0; i < sourceCount; i++)
            {
                indices[i] = i;
            }

            for (var i = 0; i < count; i++)
            {
                var j = random.Next(sourceCount - i);
                result[i] = source[indices[j]];
                indices[j] = indices[sourceCount - i - 1];
            }

            return result;
        }
    }
}
