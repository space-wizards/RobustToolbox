using System;

namespace Robust.Shared.Random;

/// <summary>
///     The global RNG. Wraps a RobustRandom, and doesn't implement <see cref="IDedicatedRandom"/>, so that
///     developers of API surfaces can require an IDedicatedRandom to avoid depending on global state.
/// </summary>
internal sealed class GlobalRandom : IRobustRandom
{
    private readonly IRobustRandom _inner = IRobustRandom.CreateRandom();

    public System.Random GetRandom()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return _inner.GetRandom();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public void SetSeed(int seed)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _inner.SetSeed(seed);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public void DebugSetSeed(int seed)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _inner.DebugSetSeed(seed);
#pragma warning restore CS0618 // Type or member is obsolete
    }


    public float NextFloat()
    {
        return _inner.NextFloat();
    }

    public int Next()
    {
        return _inner.Next();
    }

    public int Next(int maxValue)
    {
        return _inner.Next(maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return _inner.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        return _inner.NextDouble();
    }

    public TimeSpan Next(TimeSpan maxTime)
    {
        return _inner.Next(maxTime);
    }

    public TimeSpan Next(TimeSpan minTime, TimeSpan maxTime)
    {
        return _inner.Next(minTime, maxTime);
    }

    public void NextBytes(byte[] buffer)
    {
        _inner.NextBytes(buffer);
    }
}
