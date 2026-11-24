using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Similar to <see cref="CompIdx"/>.
/// </summary>
internal static class EventIdx
{
    private static int _master = -1;

    private static readonly ConcurrentDictionary<Type, int> ByType = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Of<T>() => Store<T>.Id;

    /// <summary>
    /// Id for a runtime type, for the non-generic raise overloads.
    /// </summary>
    public static int Of(Type type)
    {
        return ByType.GetOrAdd(type, static t => (int) typeof(Store<>)
            .MakeGenericType(t)
            .GetField(nameof(Store<int>.Id), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!);
    }

    private static class Store<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int Id = Interlocked.Increment(ref _master);
    }
}
