using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// A custom type serializer for reading a set of types that inherit from some base type.
/// </summary>
public sealed class AbstractDictionarySerializer<TValue> : ITypeSerializer<Dictionary<Type, TValue>, MappingDataNode>
    where TValue : notnull
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();
        foreach (var (key, valueNode) in node.Children)
        {
            var type = serializationManager.ReflectionManager.YamlTypeTagLookup(typeof(TValue), key);
            if (type == null)
            {
                mapping.Add(new ErrorNode(node.GetKeyNode(key), $"Could not resolve type: {key}"), new ValidatedValueNode(valueNode));
                continue;
            }

            mapping.Add(new ValidatedValueNode(node.GetKeyNode(key)), serializationManager.ValidateNode(type, valueNode, context));
        }

        return new ValidatedMappingNode(mapping);
    }

    public Dictionary<Type, TValue> Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Dictionary<Type, TValue>>? instanceProvider = null)
    {
        var dict = instanceProvider != null ? instanceProvider() : new Dictionary<Type, TValue>();
        foreach (var (key, valueNode) in node.Children)
        {
            var type = serializationManager.ReflectionManager.YamlTypeTagLookup(typeof(TValue), key)!;
            var value = (TValue) serializationManager.Read(type, valueNode, hookCtx, context, notNullableOverride:true)!;
            dict.Add(type, value);
        }

        return dict;
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<Type, TValue> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var mappingNode = new MappingDataNode();

        foreach (var (key, val) in value)
        {
            // TODO SERIALIZATION
            // Add some way to directly return a string w/o allocating a ValueDataNode
            var keyNode = serializationManager.WriteValue(key.Name, alwaysWrite, context, notNullableOverride: true);
            if (keyNode is not ValueDataNode valueNode)
                throw new NotSupportedException();

            mappingNode.Add(
                valueNode.Value,
                serializationManager.WriteValue(key, val, alwaysWrite, context));
        }

        return mappingNode;
    }
}
