using System;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

/// <summary>
/// Wrapper for <see cref="Random"/>.
/// </summary>
/// <remarks>
/// This should not contain any logic, not directly related to calling specific methods of <see cref="Random"/>.
/// To write additional logic, attached to random roll, please create interface-implemented methods on <see cref="IRobustRandom"/>
/// or add it to <see cref="RandomExtensions"/>.
/// </remarks>
public sealed class RobustRandom : IRobustRandom
{
    private System.Random _random = new();

    public System.Random GetRandom() => _random;

    public void SetSeed(int seed)
    {
        _random = new(seed);
    }

    public float NextFloat()
    {
        return _random.NextFloat();
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
        DebugTools.Assert(minTime < maxTime);
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
