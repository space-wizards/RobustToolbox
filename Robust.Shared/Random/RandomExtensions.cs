using System;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Random
{
    public static class RandomExtensions
    {
        /// <summary>
        ///     Generate a random number from a normal (gaussian) distribution.
        /// </summary>
        /// <param name="random">The random object to generate the number from.</param>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        public static double NextGaussian(this IRobustRandom random, double μ = 0, double σ = 1)
        {
            // https://stackoverflow.com/a/218600
            var α = random.NextDouble();
            var β = random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

            return μ + σ * randStdNormal;
        }

        public static T Pick<T>(this IRobustRandom random, IReadOnlyList<T> list)
        {
            var index = random.Next(list.Count);
            return list[index];
        }

        /// <summary>Picks a random element from a collection.</summary>
        /// <remarks>
        ///     This is O(n).
        /// </remarks>
        public static T Pick<T>(this IRobustRandom random, IReadOnlyCollection<T> collection)
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

            throw new InvalidOperationException("This should be unreachable!");
        }

        public static T PickAndTake<T>(this IRobustRandom random, IList<T> list)
        {
            var index = random.Next(list.Count);
            var element = list[index];
            list.RemoveAt(index);
            return element;
        }

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

            return random.NextDouble() <= chance;
        }
    }
}
