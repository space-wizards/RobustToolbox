using System;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

/// <summary>
/// <para>
///     Provides random numbers, can be constructed in user code to provide <see cref="IDedicatedRandom"/>s.
///     Methods that take RNG as input should take an <see cref="IRobustRandom"/> or <see cref="IDedicatedRandom"/>, not
///     a RobustRandom.
/// </para>
/// <para>
///     If you just want the global randomizer, you can depend on it through <see cref="IRobustRandom"/>, but be aware
///     that is not a <see cref="RobustRandom"/> and the implementation is internal.
/// </para>
/// </summary>
/// <example>
/// <code>
///     // Optionally, seed your RNG. By default, the RNG is seeded randomly.
///     var myRng = new IRobustRandom.CreateSeeded(17);
///     <br/>
///     var fairDiceRoll = myRng.Next(1, 6); // Will be 4 with this seed.
/// </code>
/// </example>
// Implementor's note: We're just making this internal, not removing it.
[Obsolete("Directly referring to RobustRandom is obsolete, it is an implementation detail and may be replaced.")]
public sealed class RobustRandom : IDedicatedRandom
{
    // This should not contain any logic, not directly related to calling specific methods of <see cref="Random"/>.
    // To write additional logic, attached to random roll, please create interface-implemented methods on <see cref="IRobustRandom"/>
    // or add it to <see cref="RandomExtensions"/>.
    private System.Random _random;

    /// <summary>
    ///     Constructs a new RobustRandom with a globally provided seed.
    /// </summary>
    [Obsolete($"The public constructor for RobustRandom is not meant for user consumption, use {nameof(IRobustRandom)}.{nameof(IRobustRandom.CreateRandom)}")]
    public RobustRandom()
    {
        _random = new();
    }

    /// <summary>
    ///     Constructs a new RobustRandom with the given seed.
    /// </summary>
    internal RobustRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    System.Random IRobustRandom.GetRandom() => _random;

    void IRobustRandom.SetSeed(int seed)
    {
#if DEBUG || ALLOW_BAD_PRACTICES
        _random = new(seed);
#endif
    }

    public void DebugSetSeed(int seed)
    {
#if DEBUG || ALLOW_BAD_PRACTICES
        _random = new System.Random(seed);
#endif
    }

    public float NextFloat()
    {
        // This is pretty much the CoreFX implementation.
        // So credits to that.
        // Except using float instead of double.
        return Next() * 4.6566128752458E-10f;
    }

    public float NextFloat(float minValue, float maxValue)
        => NextFloat() * (maxValue - minValue) + minValue;

    public float NextFloat(float maxValue)
        => NextFloat() * maxValue;

    public int Next()
    {
        return _random.Next();
    }

    public int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    public TimeSpan Next(TimeSpan minTime, TimeSpan maxTime)
    {
        DebugTools.Assert(minTime <= maxTime);
        return minTime + (maxTime - minTime) * _random.NextDouble();
    }

    public TimeSpan Next(TimeSpan maxTime)
    {
        return Next(TimeSpan.Zero, maxTime);
    }

    public int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }

    public void NextBytes(byte[] buffer)
    {
        _random.NextBytes(buffer);
    }
}
