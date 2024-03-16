using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

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

    /// <inheritdoc />
    public void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <inheritdoc />
    public void Shuffle<T>(Span<T> list)
    {
        var n = list.Length;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <inheritdoc />
    public void Shuffle<T>(ValueList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <inheritdoc />
    public T GetItem<T>(IList<T> collection)
    {
        var index = Next(collection.Count - 1);
        return collection[index];
    }

    /// <inheritdoc />
    public T GetItem<T>(ValueList<T> list)
    {
        var index = Next(list.Count - 1);
        return list[index];
    }

    /// <inheritdoc />
    public T GetItem<T>(Span<T> span)
    {
        var index = Next(span.Length - 1);
        return span[index];
    }

    /// <inheritdoc />
    public IReadOnlyCollection<T> GetItems<T>(IList<T> collection, int count, bool allowDuplicates = true)
    {
        if (collection.Count == 0 || count <= 0)
        {
            return Array.Empty<T>();
        }

        if (allowDuplicates == false)
        {
            if (collection.Count >= count)
            {
                var arr = collection.ToArray();
                Shuffle(arr);
                return arr;
            }
        }

        var maxCollectionIndex = collection.Count - 1;

        var rolled = new T[count];
        var selectedIndexes = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            var index = Next(maxCollectionIndex);
            var unique = selectedIndexes.Add(index);
            if (!unique && allowDuplicates)
            {
                do
                {
                    index = index == collection.Count
                        ? 0
                        : index + 1;
                } while (selectedIndexes.Add(index));
            }

            rolled[i] = collection[index];
        }

        return rolled;
    }

    /// <inheritdoc />
    public ValueList<T> GetItems<T>(ValueList<T> collection, int count, bool allowDuplicates = true)
    {
        if (collection.Count == 0 || count <= 0)
        {
            return new(0);
        }

        if (allowDuplicates == false)
        {
            if (collection.Count >= count)
            {
                var arr = collection.ToArray();
                Shuffle(arr);
                return ValueList<T>.OwningArray(arr);
            }
        }

        var maxCollectionIndex = collection.Count - 1;

        var rolled = new T[count];
        var selectedIndexes = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            var index = Next(maxCollectionIndex);
            var unique = selectedIndexes.Add(index);
            if (!unique && allowDuplicates)
            {
                do
                {
                    index = index == collection.Count
                        ? 0
                        : index + 1;
                } while (selectedIndexes.Add(index));
            }

            rolled[i] = collection[index];
        }

        return ValueList<T>.OwningArray(rolled);
    }

    /// <inheritdoc />
    public Span<T> GetItems<T>(Span<T> collection, int count, bool allowDuplicates = true)
    {
        if (collection.Length == 0 || count <= 0)
        {
            return default;
        }

        if (allowDuplicates == false)
        {
            if (collection.Length >= count)
            {
                var arr = collection.ToArray();
                Shuffle(arr);
                return arr;
            }
        }

        var maxCollectionIndex = collection.Length - 1;

        var rolled = new T[count];
        var selectedIndexes = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            var index = Next(maxCollectionIndex);
            var unique = selectedIndexes.Add(index);
            if (!unique && allowDuplicates)
            {
                do
                {
                    index = index == collection.Length
                        ? 0
                        : index + 1;
                } while (selectedIndexes.Add(index));
            }

            rolled[i] = collection[index];
        }

        return rolled;
    }
}
