using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

[TypeSerializer]
public sealed class DictionarySerializer<TKey, TValue> :
    ITypeSerializer<Dictionary<TKey, TValue>, MappingDataNode>,
    ITypeSerializer<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>,
    ITypeSerializer<SortedDictionary<TKey, TValue>, MappingDataNode>,
    ITypeSerializer<FrozenDictionary<TKey, TValue>, MappingDataNode>,
    ITypeCopier<Dictionary<TKey, TValue>>,
    ITypeCopier<SortedDictionary<TKey, TValue>>,
    ITypeCopyCreator<IReadOnlyDictionary<TKey, TValue>>,
    ITypeCopyCreator<FrozenDictionary<TKey, TValue>>
    where TKey : notnull
{
    #region Validate

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return Validate(serializationManager, node, context);
    }

    ValidationNode ITypeValidator<SortedDictionary<TKey, TValue>, MappingDataNode>.Validate(
        ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return Validate(serializationManager, node, context);
    }

    ValidationNode ITypeValidator<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Validate(
        ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return Validate(serializationManager, node, context);
    }

    ValidationNode ITypeValidator<Dictionary<TKey, TValue>, MappingDataNode>.Validate(
        ISerializationManager serializationManager,
        MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        return Validate(serializationManager, node, context);
    }

    ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        ISerializationContext? context)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();
        foreach (var (key, val) in node.Children)
        {
            mapping.Add(serializationManager.ValidateNode<TKey>(node.GetKeyNode(key), context),
                serializationManager.ValidateNode<TValue>(val, context));
        }

        return new ValidatedMappingNode(mapping);
    }

    #endregion

    #region Write

    private MappingDataNode InterfaceWrite(
        ISerializationManager serializationManager,
        IReadOnlyDictionary<TKey, TValue> value,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var mappingNode = new MappingDataNode();
        foreach (var (key, val) in value)
        {
            // TODO SERIALIZATION
            // Add some way to directly return a string w/o allocating a ValueDataNode
            var keyNode = serializationManager.WriteValue(key, alwaysWrite, context);
            if (keyNode is not ValueDataNode valueNode)
                throw new NotSupportedException("Yaml mapping keys must serialize to a ValueDataNode (i.e. a string)");

            mappingNode.Add(
                valueNode.Value,
                serializationManager.WriteValue(val, alwaysWrite, context));
        }

        return mappingNode;
    }


    public DataNode Write(ISerializationManager serializationManager, FrozenDictionary<TKey, TValue> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return InterfaceWrite(serializationManager, value, alwaysWrite, context);
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<TKey, TValue> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return InterfaceWrite(serializationManager, value, alwaysWrite, context);
    }

    public DataNode Write(ISerializationManager serializationManager, SortedDictionary<TKey, TValue> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return InterfaceWrite(serializationManager, value, alwaysWrite, context);
    }

    public DataNode Write(ISerializationManager serializationManager, IReadOnlyDictionary<TKey, TValue> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return InterfaceWrite(serializationManager, value.ToDictionary(k => k.Key, v => v.Value), alwaysWrite, context);
    }

    #endregion

    #region Read

    public Dictionary<TKey, TValue> Read(ISerializationManager serializationManager,
        MappingDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<Dictionary<TKey, TValue>>? instanceProvider)
    {
        var dict = instanceProvider != null ? instanceProvider() : new Dictionary<TKey, TValue>();

        var keyNode = new ValueDataNode();
        foreach (var (key, value) in node.Children)
        {
            keyNode.Value = key;
            dict.Add(serializationManager.Read<TKey>(keyNode, hookCtx, context),
                serializationManager.Read<TValue>(value, hookCtx, context));
        }

        return dict;
    }

    public FrozenDictionary<TKey, TValue> Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<FrozenDictionary<TKey, TValue>>? instanceProvider = null)
    {
        if (instanceProvider != null)
        {
            Logger.Warning(
                $"Provided value to a Read-call for a {nameof(FrozenDictionary<TKey, TValue>)}. Ignoring...");
        }

        var array = new KeyValuePair<TKey, TValue>[node.Children.Count];
        int i = 0;
        var keyNode = new ValueDataNode();
        foreach (var (key, value) in node.Children)
        {
            keyNode.Value = key;
            var k = serializationManager.Read<TKey>(keyNode, hookCtx, context);
            var v = serializationManager.Read<TValue>(value, hookCtx, context);
            array[i++] = new(k,v);
        }

        return array.ToFrozenDictionary();
    }


    IReadOnlyDictionary<TKey, TValue> ITypeReader<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Read(
        ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<IReadOnlyDictionary<TKey, TValue>>? instanceProvider)
    {
        if (instanceProvider != null)
        {
            Logger.Warning(
                $"Provided value to a Read-call for a {nameof(IReadOnlyDictionary<TKey, TValue>)}. Ignoring...");
        }

        var dict = new Dictionary<TKey, TValue>();

        var keyNode = new ValueDataNode();
        foreach (var (key, value) in node.Children)
        {
            keyNode.Value = key;
            dict.Add(serializationManager.Read<TKey>(keyNode, hookCtx, context),
                serializationManager.Read<TValue>(value, hookCtx, context));
        }

        return dict;
    }

    SortedDictionary<TKey, TValue> ITypeReader<SortedDictionary<TKey, TValue>, MappingDataNode>.Read(
        ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<SortedDictionary<TKey, TValue>>? instanceProvider)
    {
        var dict = instanceProvider != null ? instanceProvider() : new SortedDictionary<TKey, TValue>();
        var keyNode = new ValueDataNode();

        foreach (var (key, value) in node.Children)
        {
            keyNode.Value = key;
            dict.Add(serializationManager.Read<TKey>(keyNode, hookCtx, context),
                serializationManager.Read<TValue>(value, hookCtx, context));
        }

        return dict;
    }

    #endregion

    #region Copy

    public void CopyTo(ISerializationManager serializationManager, Dictionary<TKey, TValue> source, ref Dictionary<TKey, TValue> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.EnsureCapacity(source.Count);
        foreach (var value in source)
        {
            target.Add(
                serializationManager.CreateCopy(value.Key, hookCtx, context),
                serializationManager.CreateCopy(value.Value, hookCtx, context));
        }
    }

    public void CopyTo(ISerializationManager serializationManager, SortedDictionary<TKey, TValue> source, ref SortedDictionary<TKey, TValue> target,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target.Clear();
        foreach (var value in source)
        {
            target.Add(
                serializationManager.CreateCopy(value.Key, hookCtx, context),
                serializationManager.CreateCopy(value.Value, hookCtx, context));
        }
    }

    public IReadOnlyDictionary<TKey, TValue> CreateCopy(ISerializationManager serializationManager, IReadOnlyDictionary<TKey, TValue> source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        var target = new Dictionary<TKey, TValue>(source.Count);
        foreach (var value in source)
        {
            target.Add(
                serializationManager.CreateCopy(value.Key, hookCtx, context),
                serializationManager.CreateCopy(value.Value, hookCtx, context));
        }

        return target;
    }

    public FrozenDictionary<TKey, TValue> CreateCopy(ISerializationManager serializationManager, FrozenDictionary<TKey, TValue> source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        var array = new KeyValuePair<TKey, TValue>[source.Count];
        int i = 0;
        foreach (var value in source)
        {
            var k = serializationManager.CreateCopy(value.Key, hookCtx, context);
            var v = serializationManager.CreateCopy(value.Value, hookCtx, context);
            array[i++] = new(k, v);
        }

        return array.ToFrozenDictionary();
    }

    #endregion
}
