using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

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
        public float NextFloat(float minValue, float maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random float between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        public float NextFloat(float maxValue) => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="byte.MaxValue"/> (exclusive).
        /// </summary>
        public byte NextByte()
            => random.NextByte(byte.MaxValue);

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        public byte NextByte(byte maxValue)
            => (byte)random.Next(maxValue);

        /// <summary>
        ///     Get a random byte between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
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
        [Obsolete("Use NextDouble instead.")]
        public double NextDouble(double maxValue)
            => random.NextDouble() * maxValue;

        /// <summary>
        ///     Get a random double between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        public double NextDouble(double minValue, double maxValue)
            => random.NextDouble() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="MathF.Tau"/> (exclusive).
        /// </summary>
        public Angle NextAngle()
            => random.NextFloat() * MathF.Tau;

        /// <summary>
        ///     Get a random angle between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        public Angle NextAngle(Angle maxValue)
            => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random angle between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        public Angle NextAngle(Angle minValue, Angle maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <param name="maxMagnitude">Max value for randomized vector magnitude (exclusive).</param>
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
        public Vector2 NextVector2(float minMagnitude, float maxMagnitude)
            => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        /// </summary>
        public Vector2 NextVector2Box(float minX, float minY, float maxX, float maxY)
            => new (random.NextFloat(minX, maxX), random.NextFloat(minY, maxY));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        ///     Box will have coordinates starting at [-<paramref name="maxAbsX"/> , -<paramref name="maxAbsY"/>]
        ///     and ending in [<paramref name="maxAbsX"/> , <paramref name="maxAbsY"/>]
        /// </summary>
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
        /// <param name="random">The random object to generate the number from.</param>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        public double NextGaussian(double μ = 0, double σ = 1)
        {
            // https://stackoverflow.com/a/218600
            var α = random.NextDouble();
            var β = random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

            return μ + σ * randStdNormal;
        }

        /// <summary>Picks a random element from a collection.</summary>
        public TItem Pick<TItem>(IReadOnlyList<TItem> list)
        {
            var index = random.Next(list.Count);
            return list[index];
        }

        /// <summary>Picks a random element from a collection.</summary>
        public ref TItem Pick<TItem>(ValueList<TItem> list)
        {
            var index = random.Next(list.Count);
            return ref list[index];
        }

        /// <summary>Picks a random element from a collection.</summary>
        /// <remarks>
        ///     This is O(n).
        /// </remarks>
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
        /// Picks a random element from a list, removes it from list and returns it.
        /// This is O(n) as it preserves the order of other items in the list.
        /// </summary>
        public TItem PickAndTake<TItem>(IList<TItem> list)
        {
            var index = random.Next(list.Count);
            var element = list[index];
            list.RemoveAt(index);
            return element;
        }

        /// <summary>Picks a random element from a collection.</summary>
        /// <remarks>
        ///     This is O(n).
        /// </remarks>
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

        /// <summary>Picks and removes a random element from a collection.</summary>
        /// <remarks>
        ///     This is O(n).
        /// </remarks>
        public TItem PickAndTakeCollection<TItem>(ICollection<TItem> set)
        {
            var tile = random.PickCollection(set);
            set.Remove(tile);
            return tile;
        }

        /// <summary>
        ///     Have a certain chance to return a boolean.
        /// </summary>
        /// <param name="random">The random instance to run on.</param>
        /// <param name="chance">The chance to pass, from 0 to 1.</param>
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
        public TItem[] GetItems<TItem>(ValueList<TItem> source, int count, bool allowDuplicates = true)
        {
            return random.GetItems(source.Span, count, allowDuplicates);
        }

        /// <inheritdoc cref="GetItems{T}(System.Collections.Generic.IList{T},int,bool)"/>
        public TItem[] GetItems<TItem>(TItem[] source, int count, bool allowDuplicates = true)
        {
            return random.GetItems(source.AsSpan(), count, allowDuplicates);
        }

        /// <inheritdoc cref="GetItems{T}(System.Collections.Generic.IList{T},int,bool)"/>
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
