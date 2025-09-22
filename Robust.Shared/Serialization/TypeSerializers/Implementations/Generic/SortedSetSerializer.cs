using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

[TypeSerializer]
public sealed class SortedSetSerializer<T> :
    ITypeSerializer<SortedSet<T>, SequenceDataNode>,
    ITypeCopyCreator<SortedSet<T>>
{
    SortedSet<T> ITypeReader<SortedSet<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<SortedSet<T>>? instanceProvider)
    {
        var set = instanceProvider != null ? instanceProvider() : new SortedSet<T>();

        foreach (var dataNode in node.Sequence)
        {
            set.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
        }

        return set;
    }

    ValidationNode ITypeValidator<SortedSet<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
        SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<T>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, SortedSet<T> value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }

    SortedSet<T> ITypeCopyCreator<SortedSet<T>>.CreateCopy(ISerializationManager serializationManager, SortedSet<T> source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context)
    {
        var target = new SortedSet<T>();

        foreach (var val in source)
        {
            target.Add(serializationManager.CreateCopy(val, hookCtx, context));
        }

        return target;
    }
}
