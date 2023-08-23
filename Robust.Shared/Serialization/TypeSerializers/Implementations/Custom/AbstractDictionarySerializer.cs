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
        foreach (var (keyNode, valueNode) in node.Children)
        {
            if (keyNode is not ValueDataNode key)
            {
                mapping.Add(new ErrorNode(keyNode, $"Expected {nameof(ValueDataNode)} but was {keyNode.GetType()}"), new ValidatedValueNode(valueNode));
                continue;
            }
            var type = serializationManager.ReflectionManager.YamlTypeTagLookup(typeof(TValue), key.Value);
            if (type == null)
            {
                mapping.Add(new ErrorNode(keyNode, $"Could not resolve type: {key.Value}"), new ValidatedValueNode(valueNode));
                continue;
            }
            
            mapping.Add(new ValidatedValueNode(key), serializationManager.ValidateNode(type, valueNode, context));
        }

        return new ValidatedMappingNode(mapping);
    }

    public Dictionary<Type, TValue> Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Dictionary<Type, TValue>>? instanceProvider = null)
    {
        var dict = instanceProvider != null ? instanceProvider() : new Dictionary<Type, TValue>();
        foreach (var (keyNode, valueNode) in node.Children)
        {
            var key = (ValueDataNode) keyNode;
            var type = serializationManager.ReflectionManager.YamlTypeTagLookup(typeof(TValue), key.Value)!;
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
            mappingNode.Add(
                serializationManager.WriteValue(key.Name, alwaysWrite, context, notNullableOverride:true),
                serializationManager.WriteValue(key, val, alwaysWrite, context));
        }

        return mappingNode;
    }
}
