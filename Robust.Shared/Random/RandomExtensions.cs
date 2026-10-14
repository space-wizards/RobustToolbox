using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

public static class RandomExtensions
{
    /// <summary>
    ///     Generate a random number from a normal (gaussian) distribution.
    /// </summary>
    /// <param name="random">The random object to generate the number from.</param>
    /// <param name="μ">The average or "center" of the normal distribution.</param>
    /// <param name="σ">The standard deviation of the normal distribution.</param>
    public static double NextGaussian(this IRobustRandom random, double μ = 0, double σ = 1)
    {
        // https://stackoverflow.com/a/218600
        var α = random.NextDouble();
        var β = random.NextDouble();

        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

        return μ + σ * randStdNormal;
    }

    /// <summary>Picks a random element from a collection.</summary>
    public static T Pick<T>(this IRobustRandom random, IReadOnlyList<T> list)
    {
        var index = random.Next(list.Count);
        return list[index];
    }

    /// <summary>Picks a random element from a collection.</summary>
    public static ref T Pick<T>(this IRobustRandom random, ValueList<T> list)
    {
        var index = random.Next(list.Count);
        return ref list[index];
    }

    /// <summary>Picks a random element from a collection.</summary>
    public static ref T Pick<T>(this System.Random random, ValueList<T> list)
    {
        var index = random.Next(list.Count);
        return ref list[index];
    }

    /// <summary>Picks a random element from a collection.</summary>
    /// <remarks>
    ///     This is O(n).
    /// </remarks>
    public static T Pick<T>(this IRobustRandom random, IReadOnlyCollection<T> collection)
    {
        var index = random.Next(collection.Count);
        var i = 0;
        foreach (var t in collection)
        {
            if (i++ == index)
            {
                return t;
            }
        }

        throw new UnreachableException("This should be unreachable!");
    }

    /// <summary>
    /// Picks a random element from a list, removes it from list and returns it.
    /// This is O(n) as it preserves the order of other items in the list.
    /// </summary>
    public static T PickAndTake<T>(this IRobustRandom random, IList<T> list)
    {
        var index = random.Next(list.Count);
        var element = list[index];
        list.RemoveAt(index);
        return element;
    }

    /// <summary>
    /// Picks a random element from a set and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T Pick<T>(this System.Random random, ICollection<T> collection)
    {
        var index = random.Next(collection.Count);
        var i = 0;
        foreach (var t in collection)
        {
            if (i++ == index)
            {
                return t;
            }
        }

        throw new UnreachableException("This should be unreachable!");
    }

    /// <summary>
    /// Picks a random element based on weights provided in the float-based dictionary and returns it.
    /// </summary>
    /// <param name="random">The IRobustRandom instance, acquired via the extension.</param>
    /// <param name="weights">The dictionary of items to pick and their weights.</param>
    /// <typeparam name="T">The generic type that the dictionary would contain.</typeparam>
    /// <returns>The picked type.</returns>
    /// <exception cref="InvalidOperationException">The error when no pick was found.</exception>
    public static T Pick<T>(this IRobustRandom random, Dictionary<T, float> weights)
        where T: notnull
    {
        var sum = weights.Values.Sum();
        var accumulated = 0f;
        var rand = random.NextFloat() * sum;

        foreach (var (key, weight) in weights)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }

        throw new InvalidOperationException("Invalid weighted pick");
    }

    /// <summary>
    /// Picks a random element based on weights provided in the float-based dictionary,
    /// removes it from the possible weights, and then returns it.
    /// </summary>
    /// <param name="random">The IRobustRandom instance, acquired via the extension.</param>
    /// <param name="weights">The dictionary of items to pick and their weights.</param>
    /// <typeparam name="T">The generic type that the dictionary would contain.</typeparam>
    /// <returns>The picked element.</returns>
    /// <exception cref="InvalidOperationException">The error when no possible element was found.</exception>
    public static T PickAndTake<T>(this IRobustRandom random, Dictionary<T, float> weights)
        where T : notnull
    {
        var pick = Pick(random, weights);
        weights.Remove(pick);
        return pick;
    }

    /// <summary>
    /// Picks a random element based on weights provided in the float-based dictionary,
    /// removes it from the possible weights, and then returns it.
    /// </summary>
    /// <param name="random">The IRobustRandom instance, acquired via the extension.</param>
    /// <param name="weights">The dictionary of items to pick and their weights.</param>
    /// <param name="pick">The element that was found, if any.</param>
    /// <typeparam name="T">The generic type that the dictionary would contain.</typeparam>
    /// <returns>Whether a picked element was found.</returns>
    public static bool TryPickAndTake<T>(this IRobustRandom random, Dictionary<T, float> weights, [NotNullWhen(true)] out T? pick)
        where T : notnull
    {
        if (weights.Count == 0)
        {
            pick = default;
            return false;
        }
        pick = PickAndTake(random, weights);
        return true;
    }

    /// <summary>
    /// Picks a random element based on weights provided in the float-based dictionary and then returns it.
    /// </summary>
    /// <param name="weights">The dictionary of items to pick and their weights.</param>
    /// <param name="random">The IRobustRandom instance, acquired via the extension.</param>
    /// <typeparam name="T">The generic type that the dictionary would contain.</typeparam>
    /// <returns>The picked element.</returns>
    /// <exception cref="InvalidOperationException">The error when no possible element was found.</exception>
    public static T Pick<T>(Dictionary<T, float> weights, IRobustRandom random)
        where T : notnull
    {
        var sum = weights.Values.Sum();
        var accumulated = 0f;
        var rand = random.NextFloat() * sum;

        foreach (var (key, weight) in weights)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }

        throw new InvalidOperationException("Invalid weighted pick");
    }

    /// <summary>
    /// Picks a random from a collection then removes it and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T PickAndTake<T>(this System.Random random, ICollection<T> set)
    {
        var tile = Pick(random, set);
        set.Remove(tile);
        return tile;
    }

    /// <summary>
    ///     Generate a random number from a normal (gaussian) distribution.
    /// </summary>
    /// <param name="random">The random object to generate the number from.</param>
    /// <param name="μ">The average or "center" of the normal distribution.</param>
    /// <param name="σ">The standard deviation of the normal distribution.</param>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static double NextGaussian(this System.Random random, double μ = 0, double σ = 1)
    {
        // https://stackoverflow.com/a/218600
        var α = random.NextDouble();
        var β = random.NextDouble();

        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

        return μ + σ * randStdNormal;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random) => NextFloat(random) * MathF.Tau;

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random, Angle minAngle, Angle maxAngle)
    {
        DebugTools.Assert(minAngle < maxAngle);
        return minAngle + (maxAngle - minAngle) * random.NextDouble();
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Vector2 NextPolarVector2(this System.Random random, float minMagnitude, float maxMagnitude)
        => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

    [Obsolete("Exists as a method directly on IRobustRandom.")]
    public static float NextFloat(this IRobustRandom random)
    {
        // This is pretty much the CoreFX implementation.
        // So credits to that.
        // Except using float instead of double.
        return random.Next() * 4.6566128752458E-10f;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random)
    {
        return random.Next() * 4.6566128752458E-10f;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random, float minValue, float maxValue)
        => random.NextFloat() * (maxValue - minValue) + minValue;

    /// <summary>
    ///     Have a certain chance to return a boolean.
    /// </summary>
    /// <param name="random">The random instance to run on.</param>
    /// <param name="chance">The chance to pass, from 0 to 1.</param>
    public static bool Prob(this IRobustRandom random, float chance)
    {
        DebugTools.Assert(chance <= 1 && chance >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        return random.NextDouble() < chance;
    }

    /// <summary>
    /// Get set amount of random items from a collection.
    /// If <paramref name="allowDuplicates"/> is false and <paramref name="source"/>
    /// is smaller then <paramref name="count"/> - returns shuffled <paramref name="source"/> clone.
    /// If <paramref name="source"/> is empty, and/or <paramref name="count"/> is 0, returns empty.
    /// </summary>
    /// <param name="random">Instance of random to invoke upon.</param>
    /// <param name="source">Collection from which items should be picked.</param>
    /// <param name="count">Number of random items to be picked.</param>
    /// <param name="allowDuplicates">If true, items are allowed to be picked more than once.</param>
    public static T[] GetItems<T>(this IRobustRandom random, IList<T> source, int count, bool allowDuplicates = true)
    {
        if (source.Count == 0 || count <= 0)
            return Array.Empty<T>();

        if (allowDuplicates == false && count >= source.Count)
        {
            var arr = source.ToArray();
            // Explicit type cast to IList<T> to avoid calling the Span<T> overload.
            // We have some tests that rely on mocking of this call, and Moq doesn't support Span<T> atm.
            // https://github.com/space-wizards/RobustToolbox/issues/6329
            random.Shuffle((IList<T>)arr);
            return arr;
        }

        var sourceCount = source.Count;
        var result = new T[count];

        if (allowDuplicates)
        {
            for (var i = 0; i < count; i++)
            {
                result[i] = source[random.Next(sourceCount)];
            }

            return result;
        }

        var indices = sourceCount <= 1024 ? stackalloc int[sourceCount] : new int[sourceCount];
        for (var i = 0; i < sourceCount; i++)
        {
            indices[i] = i;
        }

        for (var i = 0; i < count; i++)
        {
            var j = random.Next(sourceCount - i);
            result[i] = source[indices[j]];
            indices[j] = indices[sourceCount - i - 1];
        }

        return result;
    }

    /// <inheritdoc cref="GetItems{T}(Robust.Shared.Random.IRobustRandom,System.Collections.Generic.IList{T},int,bool)"/>
    public static T[] GetItems<T>(this IRobustRandom random, ValueList<T> source, int count, bool allowDuplicates = true)
    {
        return GetItems(random, source.Span, count, allowDuplicates);
    }

    /// <inheritdoc cref="GetItems{T}(Robust.Shared.Random.IRobustRandom,System.Collections.Generic.IList{T},int,bool)"/>
    public static T[] GetItems<T>(this IRobustRandom random, T[] source, int count, bool allowDuplicates = true)
    {
        return GetItems(random, source.AsSpan(), count, allowDuplicates);
    }

    /// <inheritdoc cref="GetItems{T}(Robust.Shared.Random.IRobustRandom,System.Collections.Generic.IList{T},int,bool)"/>
    public static T[] GetItems<T>(this IRobustRandom random, Span<T> source, int count, bool allowDuplicates = true)
    {
        if (source.Length == 0 || count <= 0)
            return Array.Empty<T>();

        if (allowDuplicates == false && count >= source.Length)
        {
            var arr = source.ToArray();
            // Explicit type cast to IList<T> to avoid calling the Span<T> overload.
            // We have some tests that rely on mocking of this call, and Moq doesn't support Span<T> atm.
            // https://github.com/space-wizards/RobustToolbox/issues/6329
            random.Shuffle((IList<T>)arr);
            return arr;
        }

        var sourceCount = source.Length;
        var result = new T[count];

        if (allowDuplicates)
        {
            // TODO RANDOM consider just using System.Random.GetItems()
            // However, the different implementations might mean that lists & arrays shuffled using the same seed
            // generate different results, which might be undesirable?
            for (var i = 0; i < count; i++)
            {
                result[i] = source[random.Next(sourceCount)];
            }

            return result;
        }

        var indices = sourceCount <= 1024 ? stackalloc int[sourceCount] : new int[sourceCount];
        for (var i = 0; i < sourceCount; i++)
        {
            indices[i] = i;
        }

        for (var i = 0; i < count; i++)
        {
            var j = random.Next(sourceCount - i);
            result[i] = source[indices[j]];
            indices[j] = indices[sourceCount - i - 1];
        }

        return result;
    }

    /// <inheritdoc cref="HashCodeCombine(IReadOnlyCollection{int})"/>
    public static int HashCodeCombine(params int[] values)
    {
        return HashCodeCombine((IReadOnlyCollection<int>)values);
    }

    /// <summary>
    /// A very simple, deterministic djb2 hash function for generating a combined seed for the random number generator.
    /// We can't use HashCode.Combine because that is initialized with a random value, creating different results on the server and client.
    /// </summary>
    /// <example>
    /// Combine the current game tick with a NetEntity Id in order to not get the same random result if this is called multiple times in the same tick.
    /// <code>
    /// var seed = RandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
    /// </code>
    /// </example>
    public static int HashCodeCombine(IReadOnlyCollection<int> values)
    {
        int hash = 5381;
        foreach (var value in values)
        {
            hash = (hash << 5) + hash + value;
        }
        return hash;
    }
}
