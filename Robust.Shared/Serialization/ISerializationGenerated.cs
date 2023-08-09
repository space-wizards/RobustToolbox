using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated
{
    public object Copy(IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null);
}

public interface ISerializationGenerated<T> : ISerializationGenerated where T : ISerializationGenerated<T>
{
    object ISerializationGenerated.Copy(IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context)
    {
        return Copy(dependencies, hooks, context);
    }

    public static sealed T Read(DataNode node, IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null)
    {
        switch (node)
        {
            case MappingDataNode mapping:
                return T.Read(mapping, dependencies, hooks, context);
            default:
                throw new NotImplementedException(); // TODO
        }
    }

    public static abstract T Read(MappingDataNode mapping, IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null);

    public new T Copy(IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null);

    public static sealed TEnum ReadEnum<TEnum>(DataNode node) where TEnum : struct, IConvertible
    {
        return node switch
        {
            ValueDataNode valueNode => Enum.Parse<TEnum>(valueNode.Value, true),
            SequenceDataNode sequenceNode => Enum.Parse<TEnum>(((ValueDataNode) sequenceNode.Sequence[0]).Value, true),
            _ => throw new InvalidNodeTypeException($"Cannot serialize node type {node.GetType()} as enum {typeof(TEnum)}")
        };
    }

    public static sealed ValueDataNode WriteEnum<TEnum>(ref TEnum @enum) where TEnum : struct, IConvertible
    {
        return new ValueDataNode(@enum.ToString());
    }

    public static sealed TValue ReadSelfSerialize<TValue>(DataNode node) where TValue : ISelfSerialize, new()
    {
        var value = new TValue();
        value.Deserialize(((ValueDataNode) node).Value);
        return value;
    }

    public static sealed TElement[] ReadEnumArray<TElement>(DataNode node) where TElement : struct, IConvertible
    {
        switch (node)
        {
            case ValueDataNode valueNode:
            {
                return new[] { Enum.Parse<TElement>(valueNode.Value, true) };
            }
            case SequenceDataNode sequenceNode:
            {
                var array = new TElement[sequenceNode.Count];
                for (var i = 0; i < sequenceNode.Count; i++)
                {
                    array[i] = Enum.Parse<TElement>(((ValueDataNode) sequenceNode[i]).Value, true);
                }

                return array;
            }
            default:
                throw new ArgumentException($"Cannot read array from data node type {node.GetType()}");
        }
    }

    public static sealed TElement[] ReadSelfSerializeArray<TElement>(DataNode node) where TElement : ISelfSerialize, new()
    {
        switch (node)
        {
            case ValueDataNode valueNode:
            {
                var element = new TElement();
                element.Deserialize(valueNode.Value);
                return new[] { element };
            }
            case SequenceDataNode sequenceNode:
            {
                var array = new TElement[sequenceNode.Count];
                for (var i = 0; i < sequenceNode.Count; i++)
                {
                    var element = new TElement();
                    element.Deserialize(((ValueDataNode) sequenceNode[i]).Value);
                    array[i] = element;
                }

                return array;
            }
            default:
                throw new ArgumentException($"Cannot read array from data node type {node.GetType()}");
        }
    }

    public static sealed TValue GetAccessorOrDefault<TValue>(TValue? value) where TValue : struct
    {
        return value!.Value;
    }

    public static sealed TValue GetAccessorOrDefault<TValue>(TValue? value) where TValue : class, new()
    {
        return value ?? new TValue();
    }

    public static sealed TValue GetValueOrDefault<TValue>(TValue? value) where TValue : struct
    {
        return value ?? default(TValue);
    }

    public static sealed TValue GetValueOrDefault<TValue>(TValue? value) where TValue : class, new()
    {
        return value ?? new TValue();
    }
}
