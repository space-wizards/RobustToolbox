using System;
using System.Reflection;
using System.Threading;

namespace Robust.Shared.IoC;

internal static class DependencyType
{
    internal static int Index = -1;

    internal static int GetIndex(Type type)
    {
        var depType = typeof(DependencyType<>).MakeGenericType(type);
        var prop = depType.GetProperty("Index", BindingFlags.Static | BindingFlags.Public)!;
        return (int) prop.GetValue(null, null)!;
    }
}

[Obsolete("Used only in source generation internally.")]
internal static class DependencyType<T>
{
    // ReSharper disable once StaticMemberInGenericType
    [Obsolete("Used only in source generation internally.")]
    internal static readonly int Index = Interlocked.Increment(ref DependencyType.Index);
}
