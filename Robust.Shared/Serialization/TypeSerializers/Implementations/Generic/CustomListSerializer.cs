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
/// This is a variation of the <see cref="ListSerializers{T}"/> that uses a custom type serializer to handle the values.
/// </summary>
public sealed class CustomListSerializer<T, TCustomSerializer>
    : ITypeSerializer<List<T>, SequenceDataNode>
    where TCustomSerializer : ITypeSerializer<T, ValueDataNode>
{
    List<T> ITypeReader<List<T>, SequenceDataNode>.Read(
        ISerializationManager seri,
        SequenceDataNode node,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? ctx,
        ISerializationManager.InstantiationDelegate<List<T>>? instanceProvider)
    {
        var list = instanceProvider != null ? instanceProvider() : new(node.Count);
        foreach (var dataNode in node)
        {
            var value = seri.Read<T, ValueDataNode, TCustomSerializer>((ValueDataNode)dataNode, hookCtx, ctx);
            list.Add(value);
        }

        return list;
    }

    ValidationNode ITypeValidator<List<T>, SequenceDataNode>.Validate(
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
        List<T> value,
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

