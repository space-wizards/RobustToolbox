using System;
using System.Reflection;
using System.Threading;

namespace Robust.Shared.IoC;

internal static class DependencyType
{
    internal static int Index = -1;

    /// <summary>
    ///     Equivalent to <see cref="DependencyType{T}.Index"/>, in cases where the type cannot be known at compile time.
    /// </summary>
    /// <param name="type">The type of the service.</param>
    /// <returns>The index for the given type of service.</returns>
    internal static int GetIndex(Type type)
    {
        var depType = typeof(DependencyType<>).MakeGenericType(type);
        var prop = depType.GetField("Index", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (int) prop.GetValue(null)!;
    }
}

/// <summary>
///     Stores the index that is used to index this service type in <see cref="DependencyCollection._servicesArray"/>,
///     if it is present in that dependency collection.
///     The index is incremented in a thread-safe manner, referencing <see cref="DependencyType.Index"/>
///     Providing a different service type will give you a different index.
/// </summary>
/// <typeparam name="T">The type of the service</typeparam>
// ReSharper disable once UnusedTypeParameter
internal static class DependencyType<T>
{
    // ReSharper disable once StaticMemberInGenericType
    internal static readonly int Index = Interlocked.Increment(ref DependencyType.Index);
}
