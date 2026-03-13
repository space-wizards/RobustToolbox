using System;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

/// <summary>
///     Provides random numbers, can be constructed in user code or used as a dependency in the form of
///     <see cref="IRobustRandom"/>. Methods that take RNG as input should take an IRobustRandom, not a RobustRandom.
/// </summary>
/// <example>
/// <code>
///     // Optionally, seed your RNG. By default, the RNG is seeded randomly.
///     var myRng = new RobustRandom(17);
///     <br/>
///     var fairDiceRoll = myRng.Next(1, 6); // Will be 4 with this seed.
/// </code>
/// </example>
public sealed class RobustRandom : IRobustRandom
{
    // This should not contain any logic, not directly related to calling specific methods of <see cref="Random"/>.
    // To write additional logic, attached to random roll, please create interface-implemented methods on <see cref="IRobustRandom"/>
    // or add it to <see cref="RandomExtensions"/>.
    private System.Random _random;

    /// <summary>
    ///     Constructs a new RobustRandom with a globally provided seed.
    /// </summary>
    public RobustRandom()
    {
        _random = new();
    }

    /// <summary>
    ///     Constructs a new RobustRandom with the given seed.
    /// </summary>
    public RobustRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    /// <summary>
    ///     Constructs a new RobustRandom seeded from another IRobustRandom.
    /// </summary>
    public RobustRandom(IRobustRandom rng)
    {
        _random = new System.Random(rng.Next());
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
