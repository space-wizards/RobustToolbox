using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

/// <summary>
/// This is a variant of the normal array serializer that uses a custom type serializer to handle the values.
/// </summary>
public sealed class CustomArraySerializer<T, TCustomSerializer> : ITypeSerializer<T[], SequenceDataNode>
    where TCustomSerializer : ITypeSerializer<T, ValueDataNode>
{
    T[] ITypeReader<T[], SequenceDataNode>.Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<T[]>? instanceProvider)
    {
        var list = new T[node.Count];
        var i = 0;
        foreach (var dataNode in node)
        {
            list[i++] = serializationManager.Read<T, ValueDataNode, TCustomSerializer>((ValueDataNode)dataNode, hookCtx, context);
        }

        return list;
    }

    ValidationNode ITypeValidator<T[], SequenceDataNode>.Validate(
        ISerializationManager seri,
        SequenceDataNode node,
        IDependencyCollection deps,
        ISerializationContext? ctx)
    {
        var list = new List<ValidationNode>(node.Count);
        foreach (var elem in node)
        {
            list.Add(seri.ValidateNode<T, ValueDataNode, TCustomSerializer>((ValueDataNode)elem, ctx));
        }
        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(
        ISerializationManager seri,
        T[] value,
        IDependencyCollection deps,
        bool alwaysWrite = false,
        ISerializationContext? ctx = null)
    {
        var sequence = new SequenceDataNode();
        foreach (var elem in value)
        {
            sequence.Add(seri.WriteValue<T, TCustomSerializer>(elem, alwaysWrite, ctx));
        }
        return sequence;
    }
}

