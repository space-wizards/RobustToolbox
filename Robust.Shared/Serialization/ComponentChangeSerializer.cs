using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NetSerializer;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Shared.Serialization;

internal sealed class ComponentChangeSerializer : IStaticTypeSerializer
{
    private static Serializer _serializer = default!;

    public static void SetSerializer(Serializer serializer)
        => _serializer = serializer;

    public bool Handles(Type type)
        => type == typeof(ComponentChange);

    public IEnumerable<Type> GetSubtypes(Type type)
        => [typeof(GameTick)];

    public MethodInfo GetStaticWriter(Type type)
        => typeof(ComponentChangeSerializer).GetMethod(nameof(Write), BindingFlags.Static | BindingFlags.Public)!;

    public MethodInfo GetStaticReader(Type type)
        => typeof(ComponentChangeSerializer).GetMethod(nameof(Read), BindingFlags.Static | BindingFlags.Public)!;

    public static void Write(Stream stream, ComponentChange value)
    {
        // Field order must match Helpers.GetFieldInfos (alphabetical): LastModifiedTick, NetID, State.
        Primitives.WritePrimitive(stream, value.LastModifiedTick.Value);
        Primitives.WritePrimitive(stream, value.NetID);

        // Reuse wrapper: splice cached bytes if materialized (>=2 consumers), else serialize the inner state
        // inline — byte-identical to the default writer, and what a single-consumer entity hits.
        if (value.State is ReusableComponentState reuse)
        {
            var bytes = reuse.Bytes;
            if (bytes != null)
                stream.Write(bytes, 0, bytes.Length);
            else
                _serializer.Serialize(stream, reuse.Inner);
            return;
        }

        // Default object path: typeId + body.
        _serializer.Serialize(stream, value.State!);
    }

    public static void Read(Stream stream, out ComponentChange value)
    {
        Primitives.ReadPrimitive(stream, out uint lastModified);
        Primitives.ReadPrimitive(stream, out ushort netId);
        _serializer.Deserialize(stream, out var stateObj);

        value = new ComponentChange(netId, stateObj as IComponentState, new GameTick(lastModified));
    }

    public static byte[] SerializeStateBytes(IComponentState state, MemoryStream scratch)
    {
        scratch.SetLength(0);
        _serializer.Serialize(scratch, state);
        return scratch.ToArray();
    }
}
