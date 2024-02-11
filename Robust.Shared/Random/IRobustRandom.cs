using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Commands.Generic;

namespace Robust.Shared.Random;

public interface IRobustRandom
{
    /// <summary>
    /// Get the underlying System.Random
    /// </summary>
    /// <returns></returns>
    System.Random GetRandom();
    void SetSeed(int seed);

    float NextFloat();
    public float NextFloat(float minValue, float maxValue)
        => NextFloat() * (maxValue - minValue) + minValue;
    public float NextFloat(float maxValue) => NextFloat() * maxValue;
    int Next();
    int Next(int minValue, int maxValue);
    TimeSpan Next(TimeSpan minTime, TimeSpan maxTime);
    TimeSpan Next(TimeSpan maxTime);
    int Next(int maxValue);
    double NextDouble();
    double NextDouble(double minValue, double maxValue) => NextDouble() * (maxValue - minValue) + minValue;
    void NextBytes(byte[] buffer);

    public Angle NextAngle() => NextFloat() * MathF.Tau;
    public Angle NextAngle(Angle minValue, Angle maxValue) => NextFloat() * (maxValue - minValue) + minValue;
    public Angle NextAngle(Angle maxValue) => NextFloat() * maxValue;

    /// <summary>
    ///     Random vector, created from a uniform distribution of magnitudes and angles.
    /// </summary>
    /// <remarks>
    ///     In general, NextVector2(1) will tend to result in vectors with smaller magnitudes than
    ///     NextVector2Box(1,1), even if you ignored any vectors with a magnitude larger than one.
    /// </remarks>
    public Vector2 NextVector2(float minMagnitude, float maxMagnitude) => NextAngle().RotateVec(new Vector2(NextFloat(minMagnitude, maxMagnitude), 0));
    public Vector2 NextVector2(float maxMagnitude = 1) => NextVector2(0, maxMagnitude);

    /// <summary>
    ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
    /// </summary>
    public Vector2 NextVector2Box(float minX, float minY, float maxX, float maxY) => new Vector2(NextFloat(minX, maxX), NextFloat(minY, maxY));
    public Vector2 NextVector2Box(float maxAbsX = 1, float maxAbsY = 1) => NextVector2Box(-maxAbsX, -maxAbsY, maxAbsX, maxAbsY);

    void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    void Shuffle<T>(Span<T> list)
    {
        var n = list.Length;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    void Shuffle<T>(ValueList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte(byte maxValue)
    {
        return NextByte(0, maxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte()
    {
        return NextByte(byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte(byte minValue, byte maxValue)
    {
        return (byte) Next(minValue, maxValue);
    }
}

public static class RandomHelpers
{
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

    public static bool Prob(this System.Random random, double chance)
    {
        return random.NextDouble() < chance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte NextByte(this System.Random random, byte maxValue)
    {
        return NextByte(random, 0, maxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte NextByte(this System.Random random)
    {
        return NextByte(random, byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte NextByte(this System.Random random, byte minValue, byte maxValue)
    {
        return (byte) random.Next(minValue, maxValue);
    }
}
