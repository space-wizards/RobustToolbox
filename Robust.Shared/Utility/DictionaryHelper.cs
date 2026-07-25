using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.Utility;

// unsafe magic fuckery
internal static class DictionaryHelper
{
    private static readonly Func<Dictionary<object, object>, int> _getInnerCount;

    static DictionaryHelper()
    {
        var _count = typeof(Dictionary<object, object>).GetField("_count", BindingFlags.NonPublic | BindingFlags.Instance);
        var dy = new DynamicMethod("Get_Count", typeof(int), [typeof(Dictionary<object, object>)]);
        var il = dy.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, _count!);
        il.Emit(OpCodes.Ret);
        _getInnerCount = dy.CreateDelegate<Func<Dictionary<object, object>, int>>();
        Debug.Assert(_getInnerCount(new Dictionary<object, object> { {new(), new()} }) == 1);
    }

    internal static Entry<TKey, TValue>[] GetEntries<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        return Unsafe.As<Dictionary<TKey, TValue>, DictionaryLayout<TKey, TValue>>(ref dictionary).Entries;
    }

    internal static int GetCount<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        return _getInnerCount(Unsafe.As<Dictionary<TKey, TValue>, Dictionary<object, object>>(ref dictionary));
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DictionaryLayout<TKey, TValue>
    {
        public int[] Buckets = null!;
        public Entry<TKey, TValue>[] Entries = null!;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Entry<TKey, TValue>
    {
        public uint HashCode;
        public int Next;
        public TKey Key;
        public TValue? Value;
    }
}
