using System;
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
///     This is a variation of the <see cref="HashSetSerializer{T}"/> that uses a custom type serializer to read the values.
/// </summary>
public sealed class CustomHashSetSerializer<T, TCustomSerializer>
    : ITypeSerializer<HashSet<T>, SequenceDataNode>
    where TCustomSerializer : ITypeSerializer<T, ValueDataNode>
    where T : new() // required for copying.
{
    HashSet<T> ITypeReader<HashSet<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        bool skipHook,
        ISerializationContext? context, HashSet<T>? set)
    {
        set ??= new HashSet<T>();

        foreach (var dataNode in node.Sequence)
        {
            var value = serializationManager.ReadWithTypeSerializer(typeof(T), typeof(TCustomSerializer), dataNode, context, skipHook);
            if (value == null)
                throw new InvalidOperationException($"{nameof(TCustomSerializer)} returned a null value when reading using a custom hashset serializer.");

            set.Add((T)value);
        }

        return set;
    }

    ValidationNode ITypeValidator<HashSet<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
        SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNodeWith(typeof(T), typeof(TCustomSerializer), elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, HashSet<T> value,
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

    public HashSet<T> Copy(ISerializationManager serializationManager, HashSet<T> source, HashSet<T> target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.EnsureCapacity(source.Count);

        foreach (var element in source)
        {
            // we have to create a new instance of T, even if this instance is never actually used?
            // Maybe this will change in the future.
            var A = new T();

            var value = serializationManager.CopyWithTypeSerializer(typeof(TCustomSerializer), element, A, context, skipHook);
            if (value == null)
                throw new InvalidOperationException($"{nameof(TCustomSerializer)} returned a null value when copying using a custom hashset serializer.");

            target.Add((T) value);
        }

        return target;
    }
}

