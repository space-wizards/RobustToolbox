using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using NetSerializer;

namespace Robust.Shared.Serialization;

internal sealed class Vector2Serializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(Vector2);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return Type.EmptyTypes;
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(Vector2Serializer).GetMethod("WritePrimitive",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
            new Type[] { typeof(Stream), type }, null)!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(Vector2Serializer).GetMethod("ReadPrimitive",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
            new Type[] { typeof(Stream), type.MakeByRefType() }, null)!;
    }

    public static void WritePrimitive(Stream stream, Vector2 value)
    {
        Primitives.WritePrimitive(stream, value.X);
        Primitives.WritePrimitive(stream, value.Y);
    }

    public static void ReadPrimitive(Stream stream, out Vector2 value)
    {
        Primitives.ReadPrimitive(stream, out float x);
        Primitives.ReadPrimitive(stream, out float y);
        value = new Vector2(x, y);
    }
}
