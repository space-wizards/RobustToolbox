using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using NetSerializer;
using Robust.Shared.Maths;

namespace Robust.Shared.Serialization;

/// <summary>
/// NetSerializer type serializer for <see cref="UnsafeFloat"/>, <see cref="UnsafeHalf"/>, and <see cref="UnsafeFloat"/>.
/// </summary>
internal sealed class NetUnsafeFloatSerializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(UnsafeFloat) || type == typeof(UnsafeDouble) || type == typeof(UnsafeHalf);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return [];
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(NetUnsafeFloatSerializer).GetMethod(nameof(Write),
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Stream), type])!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(NetUnsafeFloatSerializer).GetMethod(nameof(Read),
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Stream), type.MakeByRefType()])!;
    }

    [UsedImplicitly]
    private static void Write(Stream stream, UnsafeFloat value)
    {
        Primitives.WritePrimitive(stream, value);
    }

    [UsedImplicitly]
    private static void Read(Stream stream, out UnsafeFloat value)
    {
        Primitives.ReadPrimitive(stream, out float readValue);
        value = readValue;
    }

    [UsedImplicitly]
    private static void Write(Stream stream, UnsafeDouble value)
    {
        Primitives.WritePrimitive(stream, value);
    }

    [UsedImplicitly]
    private static void Read(Stream stream, out UnsafeDouble value)
    {
        Primitives.ReadPrimitive(stream, out double readValue);
        value = readValue;
    }

    [UsedImplicitly]
    private static void Write(Stream stream, UnsafeHalf value)
    {
        Primitives.WritePrimitive(stream, value);
    }

    [UsedImplicitly]
    private static void Read(Stream stream, out UnsafeHalf value)
    {
        Primitives.ReadPrimitive(stream, out Half readValue);
        value = readValue;
    }
}
