using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using NetSerializer;

namespace Robust.Shared.Serialization;

internal sealed class Matrix3x2Serializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(Matrix3x2);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return Type.EmptyTypes;
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(Matrix3x2Serializer).GetMethod("WritePrimitive",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
            new Type[] { typeof(Stream), type }, null)!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(Matrix3x2Serializer).GetMethod("ReadPrimitive",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
            new Type[] { typeof(Stream), type.MakeByRefType() }, null)!;
    }

    public static void WritePrimitive(Stream stream, Matrix3x2 value)
    {
        Primitives.WritePrimitive(stream, value.M11);
        Primitives.WritePrimitive(stream, value.M12);
        Primitives.WritePrimitive(stream, value.M21);
        Primitives.WritePrimitive(stream, value.M22);
        Primitives.WritePrimitive(stream, value.M31);
        Primitives.WritePrimitive(stream, value.M32);
    }

    public static void ReadPrimitive(Stream stream, out Matrix3x2 value)
    {
        Span<float> buf = stackalloc float[6];
        for (int i = 0; i < 6; i++)
        {
            Primitives.ReadPrimitive(stream, out buf[i]);
        }
        value = new Matrix3x2(buf[0], buf[1], buf[2], buf[3], buf[4], buf[5]);
    }
}
