using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

/// <summary>
/// This serializer can be used to shorten the YAML tag used to specify the desired object type when abstract base types
/// are expected. I.e.: !type:VeryLongNamedThingConcreteAction -> !ConcreteAction
/// The name of the base type must end with 'Base' for this serializer to work.
/// Also supports most common collections directly (Array, List, HashSet and Queue).
/// </summary>
/// <typeparam name="TBase">The base type of the data field.</typeparam>
/// <exception cref="InvalidMappingException">Thrown when TBase does not end in 'Base' or the YAML tag
/// hasn't been specified using '!'.</exception>
public sealed class CustomBaseTypeSerializer<TBase> :
    ITypeSerializer<TBase, MappingDataNode>,
    ITypeSerializer<TBase, ValueDataNode>,
    ITypeSerializer<TBase[], SequenceDataNode>,
    ITypeSerializer<List<TBase>, SequenceDataNode>,
    ITypeSerializer<HashSet<TBase>, SequenceDataNode>,
    ITypeSerializer<Queue<TBase>, SequenceDataNode>
    where TBase : notnull
{
    // CustomType|Base -> CustomType
    private static readonly string BaseNameWithoutBase = ReplaceLast(typeof(TBase).Name, "Base", string.Empty);

    private static bool EndsWithBase()
    {
        return typeof(TBase).Name.EndsWith("Base");
    }

    private static string ExpandName(string name)
    {
        // !ConcreteThing -> !type:CustomTypeConcreteThing
        return name.Contains(':')
            ? name
            : name.Replace("!", $"!type:{BaseNameWithoutBase}");
    }

    private static void ThrowOnInvalidName(DataNode node)
    {
        if (EndsWithBase())
            return;

        throw new InvalidMappingException(
            $"{node.Start}: the base type of this type must end in 'Base'.");
    }

    private static void ThrowOnNullTag(DataNode node)
    {
        if (node.Tag != null)
            return;

        throw new InvalidMappingException(
            $"{node.Start}: Node does not have a tag (value starting with '!').");
    }

    private static string ReplaceLast(string currentString, string stringToReplace, string replacement)
    {
        var lastStart = currentString.LastIndexOf(stringToReplace, StringComparison.Ordinal);
        return currentString.Remove(lastStart, stringToReplace.Length) + replacement;
    }

    private static TBase ReadDataNode(
        ISerializationManager serializationManager,
        DataNode node,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TBase>? instanceProvider = null)
    {
        ThrowOnInvalidName(node);
        ThrowOnNullTag(node);
        node.Tag = ExpandName(node.Tag!);
        return serializationManager.Read(node, hookCtx, context, instanceProvider);
    }

    private static IEnumerable<TBase> ReadSequence(
        ISerializationManager serializationManager,
        SequenceDataNode nodes,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        foreach (var node in nodes)
        {
            yield return ReadDataNode(serializationManager, node, hookCtx, context);
        }
    }

    private static ValidationNode ValidateDataNode(
        ISerializationManager serializationManager,
        DataNode node,
        ISerializationContext? context = null)
    {
        if (node.Tag == null)
            return new ErrorNode(node, "Node does not have a tag (value starting with '!').");
        var copy = node.Copy();
        copy.Tag = ExpandName(node.Tag);
        return serializationManager.ValidateNode<TBase>(copy, context);
    }

    private static DataNode WriteDataNode(
        ISerializationManager serializationManager,
        TBase value,
        bool alwaysWrite,
        ISerializationContext? context = null)
    {
        DebugTools.Assert(EndsWithBase());
        var node = serializationManager.WriteValue(value.GetType(), value, alwaysWrite, context);
        DebugTools.Assert(node.Tag == null);
        node.Tag = '!' + ReplaceLast(value.GetType().Name, BaseNameWithoutBase, string.Empty);
        return node;
    }

    private static SequenceDataNode WriteSequence(
        ISerializationManager serializationManager,
        IEnumerable<TBase> values,
        bool alwaysWrite,
        ISerializationContext? context = null)
    {
        DebugTools.Assert(EndsWithBase());
        var sequence = new SequenceDataNode();
        foreach (var value in values)
        {
            sequence.Add(WriteDataNode(serializationManager, value, alwaysWrite, context));
        }
        return sequence;
    }

    public TBase Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TBase>? instanceProvider = null)
    {
        return ReadDataNode(serializationManager, node, hookCtx, context, instanceProvider);
    }

    public TBase Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TBase>? instanceProvider = null)
    {
        return ReadDataNode(serializationManager, node, hookCtx, context, instanceProvider);
    }

    public TBase[] Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TBase[]>? instanceProvider = null)
    {
        return ReadSequence(serializationManager, node, hookCtx, context).ToArray();
    }

    public List<TBase> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<TBase>>? instanceProvider = null)
    {
        return ReadSequence(serializationManager, node, hookCtx, context).ToList();
    }

    public HashSet<TBase> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<HashSet<TBase>>? instanceProvider = null)
    {
        return ReadSequence(serializationManager, node, hookCtx, context).ToHashSet();
    }

    public Queue<TBase> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Queue<TBase>>? instanceProvider = null)
    {
        return new Queue<TBase>(ReadSequence(serializationManager, node, hookCtx, context));
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>(node.Count);
        foreach (var elem in node)
        {
            list.Add(ValidateDataNode(serializationManager, elem, context));
        }
        return new ValidatedSequenceNode(list);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return ValidateDataNode(serializationManager, node, context);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return ValidateDataNode(serializationManager, node, context);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        TBase value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteDataNode(serializationManager, value, alwaysWrite, context);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        TBase[] value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteSequence(serializationManager, value, alwaysWrite, context);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        List<TBase> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteSequence(serializationManager, value, alwaysWrite);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        HashSet<TBase> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteSequence(serializationManager, value, alwaysWrite);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        Queue<TBase> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteSequence(serializationManager, value, alwaysWrite);
    }
}
