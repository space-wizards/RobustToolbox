using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NetSerializer;

namespace Robust.Shared.GameObjects;

public class IEntitySerializer : ITypeSerializer, IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(IEntity);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        yield break;
    }

    public MethodInfo? GetStaticWriter(Type type)
    {
        return this.GetType().GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
    }

    public MethodInfo? GetStaticReader(Type type)
    {
        return this.GetType().GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public);
    }

    public static void Serialize(Serializer serializer, Stream stream, object ob)
    {
    }

    public static void Deserialize(Serializer serializer, Stream stream, out object? ob)
    {
        ob = null;
    }
}
