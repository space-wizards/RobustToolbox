using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

/// <summary>
///     This is a variation of the <see cref="QueueSerializer{T}"/> that uses a custom type serializer to read/write the values.
/// </summary>
public sealed class CustomQueueSerializer<T, TCustomSerializer>
    : ITypeSerializer<Queue<T>, SequenceDataNode>
    where TCustomSerializer : ITypeSerializer<T, ValueDataNode>
    where T : new() // required for copying.
{
    Queue<T> ITypeReader<Queue<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        bool skipHook,
        ISerializationContext? context, Queue<T>? queue)
    {
        queue ??= new Queue<T>();

        foreach (var dataNode in node.Sequence)
        {
            var value = serializationManager.ReadWithTypeSerializer(typeof(T), typeof(TCustomSerializer), dataNode, context, skipHook);
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
            list.Add(serializationManager.ValidateNodeWith(typeof(T), typeof(TCustomSerializer), elem, context));
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
            sequence.Add(serializationManager.WriteWithTypeSerializer(typeof(T), typeof(TCustomSerializer), elem, alwaysWrite, context));
        }

        return sequence;
    }

    public Queue<T> Copy(ISerializationManager serializationManager, Queue<T> source, Queue<T> target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.EnsureCapacity(source.Count);

        foreach (var element in source)
        {
            var value = serializationManager.CopyWithTypeSerializer(typeof(TCustomSerializer), element, new T(), context, skipHook);
            if (value == null)
                throw new InvalidOperationException($"{nameof(TCustomSerializer)} returned a null value when copying using a custom queue serializer.");

            target.Enqueue((T) value);
        }

        return target;
    }
}

