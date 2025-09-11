using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NetSerializer;

namespace Robust.Shared.Serialization;

internal sealed class NetMathSerializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(Vector2)
            || type == typeof(Vector3)
            || type == typeof(Vector4)
            || type == typeof(Quaternion)
            || type == typeof(Matrix4x4)
            || type == typeof(Matrix3x2);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return Type.EmptyTypes;
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(NetMathSerializer)
            .GetMethod(nameof(WriteFloatObject), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(type);
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(NetMathSerializer)
            .GetMethod(nameof(ReadFloatObject), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(type);
    }

    public static void WriteFloatObject<T>(Stream stream, T value) where T : unmanaged
    {
        var floatSpan = MemoryMarshal.Cast<T, float>(new Span<T>(ref value));
        foreach (var f in floatSpan)
        {
            Primitives.WritePrimitive(stream, f);
        }
    }

    public static void ReadFloatObject<T>(Stream stream, out T value) where T : unmanaged
    {
        Unsafe.SkipInit(out value);
        var floatSpan = MemoryMarshal.Cast<T, float>(new Span<T>(ref value));
        for (var i = 0; i < floatSpan.Length; i++)
        {
            Primitives.ReadPrimitive(stream, out floatSpan[i]);
        }
    }
}
