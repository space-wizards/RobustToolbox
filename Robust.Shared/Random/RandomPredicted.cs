using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Random;

public static class RandomPredicted
{
    /// <summary>
    /// Get a predictable Random instance seeded with the current tick.
    /// This ensures identical random sequences on both client and server.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <remarks>
    /// NOTE: This means the client can guess what the number will be based on the current tick,
    /// do NOT use this for sensitive applications.
    /// </remarks>
    public static System.Random GetPredictedRandom(this IRobustRandom random, IGameTiming timing)
    {
        return new System.Random((int)timing.CurTick.Value);
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value between 0 (included) and 1 (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing)
    {
        return random.GetPredictedRandom(timing).NextFloat();
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing, float minValue, float maxValue)
    {
        return random.GetPredictedRandom(timing).NextFloat(minValue, maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing, float maxValue)
    {
        return random.GetPredictedRandom(timing).NextFloat() * maxValue;
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static int NextPredicted(this IRobustRandom random, IGameTiming timing)
    {
        return random.GetPredictedRandom(timing).Next();
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static int NextPredicted(this IRobustRandom random, IGameTiming timing, int maxValue)
    {
        return random.GetPredictedRandom(timing).Next(maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static int NextPredicted(this IRobustRandom random, IGameTiming timing, int minValue, int maxValue)
    {
        return random.GetPredictedRandom(timing).Next(minValue, maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="double"/> value between 0 (included) and 1 (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static double NextDoublePredicted(this IRobustRandom random, IGameTiming timing)
    {
        return random.GetPredictedRandom(timing).NextDouble();
    }

    /// <summary>
    /// Get predictable random <see cref="double"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static double NextDoublePredicted(this IRobustRandom random, IGameTiming timing, double minValue, double maxValue)
    {
        return random.GetPredictedRandom(timing).NextDouble() * (maxValue - minValue) + minValue;
    }

    /// <summary>
    /// Get predictable random <see cref="byte"/> value between 0 (included) and <see cref="byte.MaxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static byte NextBytePredicted(this IRobustRandom random, IGameTiming timing)
    {
        return (byte)random.GetPredictedRandom(timing).Next(byte.MaxValue);
    }

    /// <summary>
    /// Predictably fill buffer with random bytes (values).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="buffer">Buffer to fill with random bytes.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static void NextBytesPredicted(this IRobustRandom random, IGameTiming timing, byte[] buffer)
    {
        random.GetPredictedRandom(timing).NextBytes(buffer);
    }

    /// <summary>
    /// Get predictable random <see cref="Angle"/> value in range of 0 (included) and <see cref="MathF.Tau"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static Angle NextAnglePredicted(this IRobustRandom random, IGameTiming timing)
    {
        return random.GetPredictedRandom(timing).NextAngle();
    }

    /// <summary>
    /// Get predictable random <see cref="Angle"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static Angle NextAnglePredicted(this IRobustRandom random, IGameTiming timing, Angle minValue, Angle maxValue)
    {
        return random.GetPredictedRandom(timing).NextAngle(minValue, maxValue);
    }

    /// <summary>
    /// Predictably shuffle a collection using the specified tick as seed.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="list">The collection to shuffle.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static void ShufflePredicted<T>(this IRobustRandom random, IGameTiming timing, IList<T> list)
    {
        var predictedRandom = random.GetPredictedRandom(timing);
        predictedRandom.Shuffle(list);
    }

    /// <summary>
    /// Picks a predictable random element from a collection.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="list">The collection to pick from.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static T PickPredicted<T>(this IRobustRandom random, IGameTiming timing, IReadOnlyList<T> list)
    {
        var index = random.NextPredicted(timing, list.Count);
        return list[index];
    }

    /// <summary>
    /// Have a certain chance to return a boolean.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="chance">The chance to pass, from 0 to 1.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    public static bool ProbPredicted(this IRobustRandom random, IGameTiming timing, float chance)
    {
        return random.NextDoublePredicted(timing) < chance;
    }
}
