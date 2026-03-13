using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Maths;

namespace Robust.Shared.Random;

/// <summary>
/// Wrapper around random number generator helping methods.
/// </summary>
public interface IRobustRandom
{
    /// <summary> Get the underlying <see cref="Random"/>.</summary>
    [Obsolete("Do not access the underlying implementation")]
    System.Random GetRandom();

    /// <summary>
    ///     Set seed for underlying <see cref="Random"/>.
    /// </summary>
    [Obsolete($"Construct a new IRobustRandom instead of setting the seed. API was removed because people kept setting the global rng seed...")]
    void SetSeed(int seed);

    /// <summary>
    ///     Set the seed for the underlying randomizer, but only in debug.
    ///     Does nothing in release.
    /// </summary>
    void DebugSetSeed(int seed);

    /// <summary> Get random <see cref="float"/> value between 0 (included) and 1 (excluded). </summary>
    float NextFloat();

    /// <summary> Get random <see cref="int"/> value. </summary>
    int Next();

    /// <summary> Get random <see cref="int"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="maxValue">Random value should be less then this value.</param>
    int Next(int maxValue);

    /// <summary> Get random <see cref="int"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded). </summary>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less then this value.</param>
    int Next(int minValue, int maxValue);

    /// <summary> Get random <see cref="double"/> value between 0 (included) and 1 (excluded). </summary>
    double NextDouble();

    /// <summary> Get random <see cref="TimeSpan"/> value in range of <see cref="TimeSpan.Zero"/> (included) and <paramref name="maxTime"/> (excluded). </summary>
    /// <param name="maxTime">Random value should be less then this value.</param>
    TimeSpan Next(TimeSpan maxTime);

    /// <summary> Get random <see cref="TimeSpan"/> value in range of <paramref name="minTime"/> (included) and <paramref name="maxTime"/> (excluded). </summary>
    /// <param name="minTime">Random value should be greater or equal to this value.</param>
    /// <param name="maxTime">Random value should be less then this value.</param>
    TimeSpan Next(TimeSpan minTime, TimeSpan maxTime);

    /// <summary> Fill buffer with random bytes (values). </summary>
    void NextBytes(byte[] buffer);
}

[Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
public static class RandomHelpers
{
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
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

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static bool Prob(this System.Random random, double chance)
    {
        return random.NextDouble() < chance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static byte NextByte(this System.Random random, byte maxValue)
    {
        return NextByte(random, 0, maxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static byte NextByte(this System.Random random)
    {
        return NextByte(random, byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static byte NextByte(this System.Random random, byte minValue, byte maxValue)
    {
        return (byte)random.Next(minValue, maxValue);
    }
}
