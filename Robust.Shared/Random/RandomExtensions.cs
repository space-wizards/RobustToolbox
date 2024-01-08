using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random
{
    public static class RandomExtensions
    {
        public static void Shuffle<T>(this System.Random random, IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n -= 1;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static bool Prob(this System.Random random, double chance)
        {
            return random.NextDouble() < chance;
        }

        /// <summary>
        /// Picks a random element from a collection and returns it.
        /// This is O(n) as it iterates the entire collection until the selected index.
        /// </summary>
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
        /// This is O(n) as it iterates the entire collection until the selected index.
        /// </summary>
        public static T PickAndTake<T>(this System.Random random, ICollection<T> set)
        {
            var tile = Pick(random, set);
            set.Remove(tile);
            return tile;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte NextByte(this System.Random random, byte maxValue)
        {
            return NextByte(random, 0, maxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte NextByte(this System.Random random)
        {
            return NextByte(random, byte.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte NextByte(this System.Random random, byte minValue, byte maxValue)
        {
            return (byte) random.Next(minValue, maxValue);
        }

        /// <summary>
        ///     Generate a random number from a normal (gaussian) distribution.
        /// </summary>
        /// <param name="random">The random object to generate the number from.</param>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        public static double NextGaussian(this IRobustRandom random, double μ = 0, double σ = 1)
        {
            return random.GetRandom().NextGaussian(μ, σ);
        }

        public static T Pick<T>(this IRobustRandom random, IReadOnlyList<T> list)
        {
            var index = random.Next(list.Count);
            return list[index];
        }

        public static ref T Pick<T>(this IRobustRandom random, ValueList<T> list)
        {
            var index = random.Next(list.Count);
            return ref list[index];
        }

        /// <summary>Picks a random element from a collection.</summary>
        /// <remarks>
        ///     This is O(n).
        /// </remarks>
        public static T Pick<T>(this IRobustRandom random, ICollection<T> collection)
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

        public static T PickAndTake<T>(this IRobustRandom random, IList<T> list)
        {
            var index = random.Next(list.Count);
            var element = list[index];
            list.RemoveAt(index);
            return element;
        }

        /// <summary>
        ///     Generate a random number from a normal (gaussian) distribution.
        /// </summary>
        /// <param name="random">The random object to generate the number from.</param>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        public static double NextGaussian(this System.Random random, double μ = 0, double σ = 1)
        {
            // https://stackoverflow.com/a/218600
            var α = random.NextDouble();
            var β = random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

            return μ + σ * randStdNormal;
        }

        public static Angle NextAngle(this System.Random random) => NextFloat(random) * MathF.Tau;

        public static float NextFloat(this IRobustRandom random)
        {
            // This is pretty much the CoreFX implementation.
            // So credits to that.
            // Except using float instead of double.
            return random.Next() * 4.6566128752458E-10f;
        }

        public static float NextFloat(this System.Random random)
        {
            return random.Next() * 4.6566128752458E-10f;
        }

        /// <summary>
        ///     Have a certain chance to return a boolean.
        /// </summary>
        /// <param name="random">The random instance to run on.</param>
        /// <param name="chance">The chance to pass, from 0 to 1.</param>
        public static bool Prob(this IRobustRandom random, float chance)
        {
            DebugTools.Assert(chance <= 1 && chance >= 0, $"Chance must be in the range 0-1. It was {chance}.");

            return random.NextDouble() < chance;
        }

        internal static void Shuffle<T>(Span<T> array, System.Random random)
        {
            var n = array.Length;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                (array[k], array[n]) =
                    (array[n], array[k]);
            }
        }
    }
}
