using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public readonly struct CompIdx : IEquatable<CompIdx>
{
    internal readonly int Value;
    internal readonly bool Dense;
    internal readonly int DenseIndex;

    internal static CompIdx Index<T>() => Store<T>.Index;

    internal static int ArrayIndex<T>() => Index<T>().Value;

    internal static CompIdx GetIndex(Type type)
    {
        return (CompIdx)typeof(Store<>)
            .MakeGenericType(type)
            .GetField(nameof(Store<int>.Index), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!;
    }

    internal static void AssignArray<T>(ref T[] array, CompIdx idx, T value)
    {
        RefArray(ref array, idx) = value;
    }

    internal static ref T RefArray<T>(ref T[] array, CompIdx idx)
    {
        var curLength = array.Length;
        if (curLength <= idx.Value)
        {
            var newLength = MathHelper.NextPowerOfTwo(Math.Max(8, idx.Value + 1));
            Array.Resize(ref array, newLength);
        }

        return ref array[idx.Value];
    }

    internal static bool GetDense<T>()
    {
        return Store<T>.Index.Dense;
    }

    internal static void SetDense<T>(bool dense, int index)
    {
        Store<T>.Index = new CompIdx(Store<T>.Index.Value, dense, index);
    }

    private static int _CompIdxMaster = -1;

    private static class Store<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static CompIdx Index = new(Interlocked.Increment(ref _CompIdxMaster));
    }

    internal CompIdx(int value)
    {
        Value = value;
    }

    internal CompIdx(int value, bool dense, int denseIndex)
    {
        Value = value;
        Dense = dense;
        DenseIndex = denseIndex;
    }

    public bool Equals(CompIdx other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is CompIdx other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(CompIdx left, CompIdx right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CompIdx left, CompIdx right)
    {
        return !left.Equals(right);
    }
}
