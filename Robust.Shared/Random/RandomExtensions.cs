using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

[PublicAPI]
public static class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        #region Primitive IRobustRandom
        /// <summary>
        ///     Get a random float between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public float NextFloat(float minValue, float maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random float between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public float NextFloat(float maxValue) => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="byte.MaxValue"/> (exclusive).
        /// </summary>
        [MustUseReturnValue]
        public byte NextByte()
            => random.NextByte(byte.MaxValue);

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public byte NextByte(byte maxValue)
            => (byte)random.Next(maxValue);

        /// <summary>
        ///     Get a random byte between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public byte NextByte(byte minValue, byte maxValue)
            => (byte)random.Next(minValue, maxValue);

        /// <summary>
        ///     Get a random double between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [Obsolete("Use NextDouble instead, this method was named incorrectly.")]
        public double Next(double maxValue)
            => random.NextDouble() * maxValue;

        /// <summary>
        ///     Get a random double between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public double NextDouble(double maxValue)
            => random.NextDouble() * maxValue;

        /// <summary>
        ///     Get a random double between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public double NextDouble(double minValue, double maxValue)
            => random.NextDouble() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="MathF.Tau"/> (exclusive).
        /// </summary>
        [MustUseReturnValue]
        public Angle NextAngle()
            => random.NextFloat() * MathF.Tau;

        /// <summary>
        ///     Get a random angle between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public Angle NextAngle(Angle maxValue)
            => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random angle between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public Angle NextAngle(Angle minValue, Angle maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <param name="maxMagnitude">Max value for randomized vector magnitude (exclusive).</param>
        [MustUseReturnValue]
        public Vector2 NextVector2(float maxMagnitude = 1)
            => random.NextVector2(0, maxMagnitude);

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <param name="minMagnitude">Min value for randomized vector magnitude (inclusive).</param>
        /// <param name="maxMagnitude">Max value for randomized vector magnitude (exclusive).</param>
        /// <remarks>
        ///     In general, NextVector2(1) will tend to result in vectors with smaller magnitudes than
        ///     NextVector2Box(1,1), even if you ignored any vectors with a magnitude larger than one.
        /// </remarks>
        [MustUseReturnValue]
        public Vector2 NextVector2(float minMagnitude, float maxMagnitude)
            => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        /// </summary>
        [MustUseReturnValue]
        public Vector2 NextVector2Box(float minX, float minY, float maxX, float maxY)
            => new (random.NextFloat(minX, maxX), random.NextFloat(minY, maxY));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        ///     Box will have coordinates starting at [-<paramref name="maxAbsX"/> , -<paramref name="maxAbsY"/>]
        ///     and ending in [<paramref name="maxAbsX"/> , <paramref name="maxAbsY"/>]
        /// </summary>
        [MustUseReturnValue]
        public Vector2 NextVector2Box(float maxAbsX = 1, float maxAbsY = 1)
            => random.NextVector2Box(-maxAbsX, -maxAbsY, maxAbsX, maxAbsY);

        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(IList<TItem> list)
        {
            if (list is TItem[] arr)
            {
                // Done to avoid significant performance dip from Moq workaround in RandomExtensions.cs,
                // doubt it matters much.
                // https://github.com/space-wizards/RobustToolbox/issues/6329
                random.Shuffle(arr);
                return;
            }

            var n = list.Count;
            while (n > 1)
            {
                n -= 1;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(Span<TItem> list)
        {
            var n = list.Length;
            while (n > 1)
            {
                n -= 1;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary> Randomly switches positions in collection. </summary>
        public void Shuffle<TItem>(ValueList<TItem> list)
        {
            random.Shuffle(list.Span);
        }
        #endregion

        /// <summary>
        ///     Generate a random number from a normal (gaussian) distribution.
        /// </summary>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        [MustUseReturnValue]
        public double NextGaussian(double μ = 0, double σ = 1)
        {
            // https://stackoverflow.com/a/218600
            var α = random.NextDouble();
            var β = random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

            return μ + σ * randStdNormal;
        }

        #region Pick, PickAndTake

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
        #endregion

        /// <summary>
        ///     Have a certain chance to return a boolean.
        /// </summary>
        /// <param name="random">The random instance to run on.</param>
        /// <param name="chance">The chance to pass, from 0 to 1.</param>
        [MustUseReturnValue]
        public bool Prob(float chance)
        {
            DebugTools.Assert(chance is <= 1 and >= 0, $"Chance must be in the range 0-1. It was {chance}.");

            return random.NextFloat() < chance;
        }

        #region GetItems
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
        #endregion
    }

    /// <summary>Picks a random element from a collection.</summary>
    [Obsolete("System.Random based APIs are deprecated.")]
    public static ref T Pick<T>(this System.Random random, ValueList<T> list)
    {
        var index = random.Next(list.Count);
        return ref list[index];
    }

    /// <summary>
    /// Picks a random element from a set and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T Pick<T>(this System.Random random, ICollection<T> collection)
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
    /// Picks a random from a collection then removes it and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T PickAndTake<T>(this System.Random random, ICollection<T> set)
    {
        var tile = Pick(random, set);
        set.Remove(tile);
        return tile;
    }

    /// <summary>
    ///     Generate a random number from a normal (gaussian) distribution.
    /// </summary>
    /// <param name="random">The random object to generate the number from.</param>
    /// <param name="μ">The average or "center" of the normal distribution.</param>
    /// <param name="σ">The standard deviation of the normal distribution.</param>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static double NextGaussian(this System.Random random, double μ = 0, double σ = 1)
    {
        // https://stackoverflow.com/a/218600
        var α = random.NextDouble();
        var β = random.NextDouble();

        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

        return μ + σ * randStdNormal;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random) => NextFloat(random) * MathF.Tau;

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random, Angle minAngle, Angle maxAngle)
    {
        DebugTools.Assert(minAngle < maxAngle);
        return minAngle + (maxAngle - minAngle) * random.NextDouble();
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Vector2 NextPolarVector2(this System.Random random, float minMagnitude, float maxMagnitude)
        => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random)
    {
        return random.Next() * 4.6566128752458E-10f;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random, float minValue, float maxValue)
        => random.NextFloat() * (maxValue - minValue) + minValue;
}
