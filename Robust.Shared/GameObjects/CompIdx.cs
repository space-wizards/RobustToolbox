using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public readonly struct CompIdx : IEquatable<CompIdx>
{
    private static readonly Dictionary<Type, CompIdx> SlowStore = new();

    internal readonly int Value;

    internal static CompIdx Index<T>() => Store<T>.Index;

    internal static void Register(Type t)
    {
        var idx = (CompIdx)typeof(Store<>)
            .MakeGenericType(t)
            .GetField(nameof(Store<int>.Index), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!;

        SlowStore[t] = idx;
    }

    internal static CompIdx Index(Type t)
    {
        return SlowStore[t];
    }

    internal static int ArrayIndex<T>() => Index<T>().Value;
    internal static int ArrayIndex(Type type) => Index(type).Value;

    internal static void AssignArray<T>(ref T[] array, CompIdx idx, T value)
    {
        RefArray(ref array, idx) = value;
    }

    internal static ref T RefArray<T>(ref T[] array, CompIdx idx)
    {
        var curLength = array.Length;
        if (curLength <= idx.Value)
        {
            var newLength = MathHelper.NextPowerOfTwo(Math.Max(8, idx.Value));
            Array.Resize(ref array, newLength);
        }

        return ref array[idx.Value];
    }

    private static int _CompIdxMaster = -1;

    private static class Store<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly CompIdx Index = new(Interlocked.Increment(ref _CompIdxMaster));
    }

    internal CompIdx(int value)
    {
        Value = value;
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
