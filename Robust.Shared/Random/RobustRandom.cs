using System;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

/// <summary>
///     Provides random numbers, can be constructed in user code or used as a dependency in the form of
///     <see cref="IRobustRandom"/>. Methods that take RNG as input should take an IRobustRandom instead.
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
    ///     Initialize the randomizer with a new, nondeterministic seed.
    /// </summary>
    public RobustRandom()
    {
        // Just init a new one with global seeding.
        _random = new();
    }

    /// <summary>
    ///     Initialize the randomizer with a specific integer seed.
    /// </summary>
    public RobustRandom(int seed)
    {
        _random = new(seed);
    }

    /// <summary>
    ///     Initialize the randomizer by seeding it with another randomizer.
    /// </summary>
    /// <param name="other"></param>
    public RobustRandom(IRobustRandom other)
    {
        _random = new(other.Next());
    }

    public void SetSeed(int seed)
    {
        _random = new(seed);
    }

    public float NextFloat()
    {
        // This is pretty much the CoreFX implementation.
        // So credits to that.
        // Except using float instead of double.
        return Next() * 4.6566128752458E-10f;
    }

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
