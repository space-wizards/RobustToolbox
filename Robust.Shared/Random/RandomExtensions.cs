using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

[PublicAPI]
public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary>
        ///     Have a certain chance to return a boolean.
        /// </summary>
        /// <param name="chance">The chance to pass, from 0 to 1.</param>
        [MustUseReturnValue]
        public bool Prob(float chance)
        {
            DebugTools.Assert(chance is <= 1 and >= 0, $"Chance must be in the range 0-1. It was {chance}.");

            return random.NextFloat() < chance;
        }
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
