using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <remarks>
    /// NOTE: This means the client can guess what the number will be based on the current tick,
    /// do NOT use this for sensitive applications.
    /// </remarks>
    [PublicAPI]
    public static System.Random GetPredictedRandom(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        var tickValue = (int) timing.CurTick.Value;

        var combinedSeed = HashCode.Combine(tickValue, seed);

        return new System.Random(combinedSeed);
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value between 0 (included) and 1 (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextFloat();
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing, float minValue, float maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextFloat(minValue, maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="float"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static float NextFloatPredicted(this IRobustRandom random, IGameTiming timing, float maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextFloat() * maxValue;
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static int NextPredicted(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).Next();
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value in range of 0 (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static int NextPredictedMax(this IRobustRandom random, IGameTiming timing, int maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).Next(maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="int"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static int NextPredictedRange(this IRobustRandom random, IGameTiming timing, int minValue, int maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).Next(minValue, maxValue);
    }

    /// <summary>
    /// Get predictable random <see cref="double"/> value between 0 (included) and 1 (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static double NextDoublePredicted(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextDouble();
    }

    /// <summary>
    /// Get predictable random <see cref="double"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static double NextDoublePredicted(this IRobustRandom random, IGameTiming timing, double minValue, double maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextDouble() * (maxValue - minValue) + minValue;
    }

    /// <summary>
    /// Get predictable random <see cref="byte"/> value between 0 (included) and <see cref="byte.MaxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static byte NextBytePredicted(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        return (byte)random.GetPredictedRandom(timing, seed).Next(byte.MaxValue);
    }

    /// <summary>
    /// Predictably fill buffer with random bytes (values).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="buffer">Buffer to fill with random bytes.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static void NextBytesPredicted(this IRobustRandom random, IGameTiming timing, byte[] buffer, int seed = 0)
    {
        random.GetPredictedRandom(timing, seed).NextBytes(buffer);
    }

    /// <summary>
    /// Get predictable random <see cref="Angle"/> value in range of 0 (included) and <see cref="MathF.Tau"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static Angle NextAnglePredicted(this IRobustRandom random, IGameTiming timing, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextAngle();
    }

    /// <summary>
    /// Get predictable random <see cref="Angle"/> value in range of <paramref name="minValue"/> (included) and <paramref name="maxValue"/> (excluded).
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="minValue">Random value should be greater or equal to this value.</param>
    /// <param name="maxValue">Random value should be less than this value.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static Angle NextAnglePredicted(this IRobustRandom random, IGameTiming timing, Angle minValue, Angle maxValue, int seed = 0)
    {
        return random.GetPredictedRandom(timing, seed).NextAngle(minValue, maxValue);
    }

    /// <summary>
    /// Predictably shuffle a collection using the specified tick as seed.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="list">The collection to shuffle.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static void ShufflePredicted<T>(this IRobustRandom random, IGameTiming timing, IList<T> list, int seed = 0)
    {
        var predictedRandom = random.GetPredictedRandom(timing, seed);
        predictedRandom.Shuffle(list);
    }

    /// <summary>
    /// Picks a predictable random element from a collection.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="list">The collection to pick from.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static T PickPredicted<T>(this IRobustRandom random, IGameTiming timing, IReadOnlyList<T> list, int seed = 0)
    {
        var index = random.NextPredictedMax(timing, list.Count, seed);
        return list[index];
    }

    /// <summary>
    /// Have a certain chance to return a boolean.
    /// </summary>
    /// <param name="random">The <see cref="IRobustRandom"/> instance.</param>
    /// <param name="timing">The <see cref="IGameTiming"/> to use for seeding.</param>
    /// <param name="chance">The chance to pass, from 0 to 1.</param>
    /// <param name="seed">Additional seed value to mix with the tick for unique sequences.</param>
    /// <inheritdoc cref="GetPredictedRandom" path="/remarks"/>
    [PublicAPI]
    public static bool ProbPredicted(this IRobustRandom random, IGameTiming timing, float chance, int seed = 0)
    {
        return random.NextDoublePredicted(timing, seed) < chance;
    }
}
