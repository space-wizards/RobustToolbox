using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using NetSerializer;
using Robust.Shared.RichText;

namespace Robust.Shared.Serialization;

/// <summary>
/// Special network serializer for <see cref="FormattedString"/> to make sure validation runs for network values.
/// </summary>
internal sealed class NetFormattedStringSerializer : IStaticTypeSerializer
{
    public bool Handles(Type type)
    {
        return type == typeof(FormattedString);
    }

    public IEnumerable<Type> GetSubtypes(Type type)
    {
        return [typeof(string)];
    }

    public MethodInfo GetStaticWriter(Type type)
    {
        return typeof(NetFormattedStringSerializer).GetMethod("Write", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    public MethodInfo GetStaticReader(Type type)
    {
        return typeof(NetFormattedStringSerializer).GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    [UsedImplicitly]
    private static void Write(Stream stream, FormattedString value)
    {
        Primitives.WritePrimitive(stream, value.Markup);
    }

    [UsedImplicitly]
    private static void Read(Stream stream, out FormattedString value)
    {
        Primitives.ReadPrimitive(stream, out string markup);

        // Must be valid formed strict markup, do not trust the client!
        value = FormattedString.FromMarkup(markup);
    }
}
