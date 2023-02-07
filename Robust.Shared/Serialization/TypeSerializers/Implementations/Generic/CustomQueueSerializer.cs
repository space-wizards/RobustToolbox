using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

/// <summary>
///     This is a variation of the <see cref="QueueSerializer{T}"/> that uses a custom type serializer to read/write the values.
/// </summary>
public sealed class CustomQueueSerializer<T, TCustomSerializer>
    : ITypeSerializer<Queue<T>, SequenceDataNode>
    where TCustomSerializer : ITypeSerializer<T, ValueDataNode>
{
    Queue<T> ITypeReader<Queue<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context, ISerializationManager.InstantiationDelegate<Queue<T>>? instanceProvider)
    {
        var queue = instanceProvider != null ? instanceProvider() : new Queue<T>();

        foreach (var dataNode in node.Sequence)
        {
            var value = serializationManager.Read<T, ValueDataNode, TCustomSerializer>((ValueDataNode)dataNode, hookCtx, context);
            if (value == null)
                throw new InvalidOperationException($"{nameof(TCustomSerializer)} returned a null value when reading using a custom hashset serializer.");

            queue.Enqueue((T)value);
        }

        return queue;
    }

    ValidationNode ITypeValidator<Queue<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
        SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<T, ValueDataNode, TCustomSerializer>((ValueDataNode)elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, Queue<T> value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue<T, TCustomSerializer>(elem, alwaysWrite, context));
        }

        return sequence;
    }
}

