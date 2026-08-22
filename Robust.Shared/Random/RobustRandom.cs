using System;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

/// <summary>
///     Provides random numbers, can be constructed in user code or used as a dependency in the form of
///     <see cref="IRobustRandom"/>. Methods that take RNG as input should take an IRobustRandom instead.
/// </summary>
/// <example>
/// <code>
///     var myRng = new RobustRandom();
///     // Optionally, seed your RNG. By default, the RNG is seeded randomly.
///     myRng.SetSeed(17);
///     <br/>
///     var fairDiceRoll = myRng.Next(1, 6); // Will be 4 with this seed.
/// </code>
/// </example>
public sealed class RobustRandom : IRobustRandom
{
    // This should not contain any logic, not directly related to calling specific methods of <see cref="Random"/>.
    // To write additional logic, attached to random roll, please create interface-implemented methods on <see cref="IRobustRandom"/>
    // or add it to <see cref="RandomExtensions"/>.
    private System.Random _random = new();

    /// <inheritdoc />
    public System.Random GetRandom() => _random;

    /// <inheritdoc />
    public void SetSeed(int seed)
    {
        _random = new(seed);
    }

    /// <inheritdoc />
    public float NextFloat()
    {
        // This is pretty much the CoreFX implementation.
        // So credits to that.
        // Except using float instead of double.
        return Next() * 4.6566128752458E-10f;
    }

    /// <inheritdoc />
    public int Next()
    {
        return _random.Next();
    }

    /// <inheritdoc />
    public int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }

    /// <inheritdoc />
    public int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    /// <inheritdoc />
    public long NextLong()
    {
        return _random.NextInt64();
    }

    /// <inheritdoc />
    public long NextLong(long maxValue)
    {
        return _random.NextInt64(maxValue);
    }

    /// <inheritdoc />
    public long NextLong(long minValue, long maxValue)
    {
        return _random.NextInt64(minValue, maxValue);
    }

    /// <inheritdoc />
    public TimeSpan Next(TimeSpan minTime, TimeSpan maxTime)
    {
        DebugTools.Assert(minTime <= maxTime);
        return minTime + (maxTime - minTime) * _random.NextDouble();
    }

    /// <inheritdoc />
    public TimeSpan Next(TimeSpan maxTime)
    {
        return Next(TimeSpan.Zero, maxTime);
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        return _random.NextDouble();
    }

    /// <inheritdoc />
    public void NextBytes(byte[] buffer)
    {
        _random.NextBytes(buffer);
    }
}
