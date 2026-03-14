using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(1).
        /// </remarks>
        /// <param name="list">The collection to pick from.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>The picked item.</returns>
        [MustUseReturnValue]
        public TItem Pick<TItem>(IReadOnlyList<TItem> list)
        {
            var index = random.Next(list.Count);
            return list[index];
        }

        /// <summary>
        ///     Tries to pick a random element from a collection, failing if it is empty.
        /// </summary>
        /// <remarks>
        ///     This is O(1).
        /// </remarks>
        /// <param name="list">The collection to pick from.</param>
        /// <param name="item">The removed item if any.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>Whether an item was successfully removed.</returns>
        public bool TryPick<TItem>(IReadOnlyList<TItem> list, [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
        {
            if (list.Count == 0)
            {
                item = default;
                return false;
            }

            item = random.Pick(list);
            return true;
        }

        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(1). This has no Try variant due to returning a ref.
        /// </remarks>
        /// <param name="list">The collection to pick from.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>The picked item.</returns>
        [MustUseReturnValue]
        public ref TItem Pick<TItem>(ValueList<TItem> list)
        {
            var index = random.Next(list.Count);
            return ref list[index];
        }

        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">The collection to pick from.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>The picked item.</returns>
        [MustUseReturnValue]
        public TItem Pick<TItem>(IReadOnlyCollection<TItem> collection)
        {
            var index = random.Next(collection.Count);
            var i = 0;
            foreach (var t in collection)
            {
                if (i++ == index)
                {
                    return t;
                }
            }

            throw new UnreachableException("This should be unreachable!");
        }

        /// <summary>
        ///     Tries to pick a random element from a collection, failing if it is empty.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">The collection to pick from.</param>
        /// <param name="item">The removed item if any.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>Whether an item was successfully removed.</returns>
        public bool TryPick<TItem>(IReadOnlyCollection<TItem> collection, [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
        {
            if (collection.Count == 0)
            {
                item = default;
                return false;
            }

            item = random.Pick(collection);
            return true;
        }

        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(n) as it preserves element order on removal.
        /// </remarks>
        /// <param name="list">The collection to pick from.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>The picked item.</returns>
        [MustUseReturnValue]
        public TItem PickAndTake<TItem>(IList<TItem> list)
        {
            var index = random.Next(list.Count);
            var element = list[index];
            list.RemoveAt(index);
            return element;
        }

        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(n) as it preserves element order on removal.
        /// </remarks>
        /// <param name="list">The collection to pick from.</param>
        /// <param name="item">The removed item, if any.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>Whether an item was successfully removed</returns>
        public bool TryPickAndTake<TItem>(IList<TItem> list, [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
        {
            if (list.Count == 0)
            {
                item = default;
                return false;
            }

            item = random.PickAndTake(list);
            return true;
        }

        /// <summary>
        ///     Picks a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">The collection to pick from.</param>
        /// <typeparam name="TItem">The type of item in the collection.</typeparam>
        /// <returns>The picked item.</returns>
        [MustUseReturnValue]
        public TItem PickCollection<TItem>(ICollection<TItem> collection)
        {
            var index = random.Next(collection.Count);
            var i = 0;
            foreach (var t in collection)
            {
                if (i++ == index)
                {
                    return t;
                }
            }

            throw new UnreachableException("This should be unreachable!");
        }

        /// <summary>
        ///     Attempts to pick from a collection, returning false if the collection is empty.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">Collection to select from.</param>
        /// <param name="item">The picked item, if any.</param>
        /// <typeparam name="TItem">The type of item within the collection.</typeparam>
        /// <returns>Whether an item was picked.</returns>
        public bool TryPickCollection<TItem>(ICollection<TItem> collection, [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
        {
            if (collection.Count == 0)
            {
                item = default;
                return false;
            }

            item = random.PickCollection(collection);
            return true;
        }

        /// <summary>
        ///     Picks and removes a random element from a collection.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">Collection to select from.</param>
        /// <typeparam name="TItem">The type of item within the collection.</typeparam>
        /// <returns>The picked item from the collection.</returns>
        [MustUseReturnValue]
        public TItem PickAndTakeCollection<TItem>(ICollection<TItem> collection)
        {
            var tile = random.PickCollection(collection);
            collection.Remove(tile);
            return tile;
        }

        /// <summary>
        ///     Attempts to pick and remove from a collection, returning false if the collection is empty.
        /// </summary>
        /// <remarks>
        ///     This is O(n) due to the input not being indexable.
        /// </remarks>
        /// <param name="collection">Collection to select from.</param>
        /// <param name="item">The picked/removed item, if any.</param>
        /// <typeparam name="TItem">The type of item within the collection.</typeparam>
        /// <returns>Whether an item was picked.</returns>
        public bool TryPickAndTakeCollection<TItem>(ICollection<TItem> collection, [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
        {
            if (random.TryPickCollection(collection, out item))
            {
                collection.Remove(item);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Pick an item from a weighted bag, where the value (<typeparamref name="TNumber"/>) is used as a relative
        ///     probability for that item.
        /// </summary>
        /// <remarks>
        ///     Items with a weight of zero will never be picked, and an assertion will be hit if all items have a weight of zero.
        /// </remarks>
        /// <typeparam name="TItem">The type of item we're picking.</typeparam>
        /// <typeparam name="TNumber">The numeric key used as a weight. This can be any floating point type.</typeparam>
        /// <returns>The item picked out of the collection.</returns>
        [MustUseReturnValue]
        public TItem PickWeighted<TItem, TNumber>(IReadOnlyCollection<KeyValuePair<TItem, TNumber>> bag)
            where TNumber : IFloatingPoint<TNumber>
        {
            var keySum = TNumber.Zero;

            foreach (var (_, key) in bag)
            {
                DebugTools.Assert(TNumber.IsPositive(key), "Keys for a weighted pick must be positive");
                keySum += key;
            }

            DebugTools.Assert(keySum > TNumber.Zero, "The sum of keys in a weighted bag should be larger than zero.");

            var threshold = keySum * TNumber.CreateChecked(random.NextDouble());

            var accumulated = TNumber.Zero;

            foreach (var (item, weight) in bag)
            {
                if (weight == TNumber.Zero)
                    continue; // Skip.

                accumulated += weight;

                if (accumulated >= threshold)
                {
                    return item;
                }
            }

            throw new InvalidOperationException("Invalid weighted pick, we shouldn't get here.");
        }

        /// <summary>
        ///     Pick an item from a weighted bag, where the value (<typeparamref name="TNumber"/>) is used as a relative
        ///     probability for that item.
        /// </summary>
        /// <remarks>
        ///     Items with a weight of zero will never be picked.
        /// </remarks>
        /// <typeparam name="TItem">The type of item we're picking.</typeparam>
        /// <typeparam name="TNumber">The numeric key used as a weight. This can be any floating point type.</typeparam>
        /// <returns>The item picked out of the collection.</returns>
        public bool TryPickWeighted<TItem, TNumber>(
            IReadOnlyCollection<KeyValuePair<TItem, TNumber>> bag,
            [NotNullWhen(true)] out TItem? item)
            where TItem : notnull
            where TNumber : IFloatingPoint<TNumber>
        {
            var keySum = TNumber.Zero;

            foreach (var (_, key) in bag)
            {
                DebugTools.Assert(TNumber.IsPositive(key), "Keys for a weighted pick must be positive");
                keySum += key;
            }

            if (keySum == TNumber.Zero)
            {
                item = default;
                return false;
            }

            var threshold = keySum * TNumber.CreateChecked(random.NextDouble());

            var accumulated = TNumber.Zero;

            foreach (var (picked, weight) in bag)
            {
                if (weight == TNumber.Zero)
                    continue; // Skip.

                accumulated += weight;

                if (accumulated >= threshold)
                {
                    item = picked;
                    return true;
                }
            }

            throw new InvalidOperationException("Invalid weighted pick, we shouldn't get here.");
        }

        /// <summary>
        ///     Pick an item from a dictionary of items to weights, where the key (<typeparamref name="TNumber"/>) is
        ///     used as a relative probability for that item.
        /// </summary>
        /// <remarks>
        ///     Items with a weight of zero will never be picked, and an assertion will be hit if all items have a weight of zero.
        /// </remarks>
        /// <typeparam name="TItem">The type of item we're picking.</typeparam>
        /// <typeparam name="TNumber">The numeric key used as a weight. This can be any floating point type.</typeparam>
        /// <returns>The item picked out of the dictionary.</returns>
        [MustUseReturnValue]
        public TItem PickAndTakeWeighted<TItem, TNumber>(Dictionary<TItem, TNumber> bag)
            where TItem: notnull
            where TNumber : IFloatingPoint<TNumber>
        {
            var item = random.PickWeighted(bag);
            bag.Remove(item);
            return item;
        }

        /// <summary>
        ///     Pick an item from a dictionary of items to weights, where the key (<typeparamref name="TNumber"/>) is
        ///     used as a relative probability for that item.
        /// </summary>
        /// <remarks>
        ///     Items with a weight of zero will never be picked, and an assertion will be hit if all items have a weight of zero.
        /// </remarks>
        /// <typeparam name="TItem">The type of item we're picking.</typeparam>
        /// <typeparam name="TNumber">The numeric key used as a weight. This can be any floating point type.</typeparam>
        /// <returns>The item picked out of the dictionary.</returns>
        public bool TryPickAndTakeWeighted<TItem, TNumber>(Dictionary<TItem, TNumber> bag, [NotNullWhen(true)] out TItem? item)
            where TItem: notnull
            where TNumber : IFloatingPoint<TNumber>
        {
            if (!random.TryPickWeighted(bag, out item))
                return false;

            bag.Remove(item);
            return true;
        }
    }
}
