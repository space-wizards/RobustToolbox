using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Random;

/// <summary>
/// <para>
///     Provides random numbers, and can be constructed using various static members that provide a <see cref="IDedicatedRandom"/>.
///     Methods that take RNG as input should take an <see cref="IRobustRandom"/> or <see cref="IDedicatedRandom"/>, instead
///     of their implementations.
/// </para>
/// <para>
///     If you just want the global randomizer, you can depend on it with IoC using <see cref="IRobustRandom"/>. This
///     will not however implement <see cref="IDedicatedRandom"/>.
/// </para>
/// </summary>
/// <example>
/// <code>
///     // Use one of the constructors on IRobustRandom to create your randomizer.
///     // This is the one for creating a randomizer with a given seed.
///     var myRng = IRobustRandom.CreateSeeded(17);
///     <br/>
///     var fairDiceRoll = myRng.Next(1, 6); // Will be 4 with this seed.
/// </code>
/// </example>
public partial interface IRobustRandom
{
    /// <summary> Get the underlying <see cref="Random"/>.</summary>
    [Obsolete("Do not access the underlying implementation, it will be changed and this will be removed.")]
    System.Random GetRandom();

    /// <summary>
    ///     Set seed for underlying <see cref="Random"/>.
    /// </summary>
    [Obsolete("Construct a new IRobustRandom instead of setting the seed. API was removed because people kept setting the global rng seed...")]
    void SetSeed(int seed);

    /// <summary>
    ///     Set the seed for the underlying randomizer, but only in debug.
    ///     Does nothing in release.
    /// </summary>
    void DebugSetSeed(int seed);

    /// <summary>
    ///     Get a random float between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    [MustUseReturnValue]
    float NextFloat();

    /// <summary>
    ///     Get a random int between 0 (inclusive) and <see cref="int.MaxValue"/> (exclusive).
    /// </summary>
    [MustUseReturnValue]
    int Next();

    /// <summary>
    ///     Get a random int between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
    /// </summary>
    /// <param name="maxValue">Exclusive upper bound on the random value.</param>
    [MustUseReturnValue]
    int Next(int maxValue);

    /// <summary>
    ///     Get a random int between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
    /// </summary>
    /// <param name="minValue">Inclusive lower bound on the random value.</param>
    /// <param name="maxValue">Exclusive upper bound on the random value.</param>
    [MustUseReturnValue]
    int Next(int minValue, int maxValue);

    /// <summary>
    ///     Get a random double between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    [MustUseReturnValue]
    double NextDouble();

    /// <summary>
    ///     Get a random <see cref="TimeSpan"/> between 0 (inclusive) and <paramref name="maxTime"/> (exclusive).
    /// </summary>
    /// <param name="maxTime">Exclusive upper bound on the random value.</param>
    [MustUseReturnValue]
    TimeSpan Next(TimeSpan maxTime);

    /// <summary>
    ///     Get a random <see cref="TimeSpan"/> between <paramref name="minTime"/> (inclusive) and <paramref name="maxTime"/> (exclusive).
    /// </summary>
    /// <param name="minTime">Inclusive lower bound on the random value.</param>
    /// <param name="maxTime">Exclusive upper bound on the random value.</param>
    [MustUseReturnValue]
    TimeSpan Next(TimeSpan minTime, TimeSpan maxTime);

    /// <summary>
    ///     Fills a given buffer with random bytes.
    /// </summary>
    void NextBytes(Span<byte> buffer);
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
