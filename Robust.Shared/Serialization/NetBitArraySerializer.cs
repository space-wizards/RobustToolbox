using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using NetSerializer;

namespace Robust.Shared.Serialization;

/// <summary>
/// Custom serializer implementation for <see cref="BitArray"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is necessary as, since .NET 10, the internal layout of <see cref="BitArray"/> was changed.
/// The type now (internally) implements <see cref="ISerializable"/> for backwards compatibility with existing
/// <c>BinaryFormatter</c> code, but NetSerializer does not support <see cref="ISerializable"/>.
/// </para>
/// <para>
/// This code is designed to be backportable &amp; network compatible with the previous behavior on .NET 9.
/// </para>
/// </remarks>
internal sealed class NetBitArraySerializer : IStaticTypeSerializer
{
    // For reference, the layout of BitArray before .NET 10 was:
    // private int[] m_array;
    // private int m_length;
    // private int _version;
    // NetSerializer serialized these in the following order (sorted by name):
    // _version, m_array, m_length

    public bool Handles(Type type)
    {
        return type == typeof(BitArray);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return [typeof(int[]), typeof(int)];
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(NetBitArraySerializer).GetMethod("Write", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(NetBitArraySerializer).GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    [UsedImplicitly]
    private static void Write(Serializer serializer, Stream stream, BitArray value)
    {
        var intCount = (31 + value.Length) >> 5;
        var ints = new int[intCount];
        value.CopyTo(ints, 0);

        serializer.SerializeDirect(stream, 0); // _version
        serializer.SerializeDirect(stream, ints); // m_array
        serializer.SerializeDirect(stream, value.Length); // m_length
    }

    [UsedImplicitly]
    private static void Read(Serializer serializer, Stream stream, out BitArray value)
    {
        serializer.DeserializeDirect<int>(stream, out _); // _version
        serializer.DeserializeDirect<int[]>(stream, out var array); // m_array
        serializer.DeserializeDirect<int>(stream, out var length); // m_length

        value = new BitArray(array)
        {
            Length = length
        };
    }
}
