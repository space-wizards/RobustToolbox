using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

[TypeSerializer]
public sealed class QueueSerializer<T> : ITypeSerializer<Queue<T>, SequenceDataNode>,  ITypeCopier<Queue<T>>
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
            queue.Enqueue(serializationManager.Read<T>(dataNode, hookCtx, context));
        }

        return queue;
    }

    ValidationNode ITypeValidator<Queue<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
        SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<T>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, Queue<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }

    public void CopyTo(ISerializationManager serializationManager, Queue<T> source, ref Queue<T> target, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target.Clear();
        target.EnsureCapacity(source.Count);

        foreach (var val in source)
        {
            target.Enqueue(serializationManager.CreateCopy(val, hookCtx, context));
        }
    }
}
