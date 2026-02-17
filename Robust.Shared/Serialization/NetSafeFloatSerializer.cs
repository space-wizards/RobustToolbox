using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NetSerializer;

namespace Robust.Shared.Serialization;

/// <summary>
/// Replaces NetSerializer's default float handling to read NaN values as 0.
/// </summary>
internal sealed class NetSafeFloatSerializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(float) || type == typeof(double) || type == typeof(Half);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return [];
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(Primitives).GetMethod(nameof(Primitives.WritePrimitive),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(Stream), type])!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(SafePrimitives).GetMethod(nameof(SafePrimitives.ReadPrimitive),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(Stream), type.MakeByRefType()])!;
    }
}
