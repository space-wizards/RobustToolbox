using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// A highly complex serializer that allows you to serialize a list with complex objects
/// as if it were an organized dictionary.
/// Should be used on a Datafield with the <see cref="AlwaysPushInheritanceAttribute"/>
/// </summary>
/// <typeparam name="T">An object which implements <see cref="IOrganizeableCollection{T}"/></typeparam>
public sealed class OrganizedListSerializer<T> : ITypeSerializer<List<T>, SequenceDataNode> where T : IOrganizeableCollection<T>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>(node.Count);
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<T>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<T> Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<T>>? instanceProvider = null)
    {
        if (instanceProvider != null)
        {
            var sawmill = dependencies.Resolve<ILogManager>().GetSawmill("szr");
            sawmill.Warning($"Provided value to a Read-call for a {nameof(List<T>)}. Ignoring...");
        }

        var list = new List<T>(node.Count);

        foreach (var sequence in node.Sequence)
        {
            list.Add(serializationManager.Read<T>(sequence, hookCtx, context));
        }

        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var modified = false;
            for (var j = list.Count - 1; j > i; j--)
            {
                var item2 = serializationManager.Read<T>(node.Sequence[j], hookCtx, context);
                if (item.Equals(item2))
                {
                    // Note that this does not organize the nested list items.
                    // If you deem that necessary, then feel free to write additional logic for that :P
                    // You'd likely want to do the equality checking at the read step rather than here if you were going to.
                    list.RemoveAt(j);
                    item.Insert(item2);
                    modified = true;
                }
            }

            if (modified)
                list[i] = item;
        }

        list.Sort();
        return list;
    }

    public DataNode Write(ISerializationManager serializationManager,
        List<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode(value.Count);

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }
}

/// <summary>
/// Interface for an object which can be organized and sorted internally.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IOrganizeableCollection<T> : IComparable<T>, IEquatable<T>
{
    /// <summary>
    /// Combines these two objects together, typically called by a <see cref="OrganizedListSerializer{T}"/>
    /// </summary>
    /// <param name="other">The other object we are combining into this one.</param>
    void Insert(T other);
}
