using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    #region DelegateElements

    private delegate ValidationNode ValidationDelegate(DataNode node, ISerializationContext? context);

    private readonly ConcurrentDictionary<(Type type, Type node), ValidationDelegate> _validationDelegates = new();

    private ValidationDelegate GetOrCreateValidationDelegate(Type type, Type nodeType)
    {
        return _validationDelegates.GetOrAdd((type, nodeType), static (key, manager) =>
        {
            var validate = Validate(manager, key.type, key.node);
            return (node, context) =>
            {
                if (!IsNull(node))
                    return validate(node, context);

                if (key.type.IsNullable())
                    return new ValidatedValueNode(node);

                return new ErrorNode(node, "Non-nullable field contained a null value");
            };
        }, this);
    }

    private static Func<DataNode, ISerializationContext?, ValidationNode> Validate(SerializationManager serialization, Type type, Type nodeType)
    {
        if (serialization._regularSerializerProvider.TryGetTypeNodeSerializer(typeof(ITypeValidator<,>),
                type,
                nodeType,
                out var serializer))
        {
            var method = typeof(SerializationManager)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.Name == nameof(ValidateNode) && m.GetGenericArguments().Length == 2)
                .MakeGenericMethod(type, nodeType);
            return (node, context) => (ValidationNode) method.Invoke(serialization, [serializer, node, context])!;
        }

        if (type.IsArray)
        {
            if (!nodeType.IsAssignableTo(typeof(SequenceDataNode)))
                return (node, _) => new ErrorNode(node, "Invalid nodetype for array.");

            var elementType = type.GetElementType();
            if (elementType == null)
                throw new ArgumentException($"Failed to get ElementType of ArrayType {type}");

            var method = typeof(SerializationManager)
                .GetMethod(nameof(ValidateArray),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    [typeof(SequenceDataNode), typeof(ISerializationContext)])!
                .MakeGenericMethod(elementType);
            return (node, context) => (ValidationNode) method.Invoke(serialization, [node, context])!;
        }

        if (type.IsEnum)
        {
            DebugTools.Assert(type != typeof(Enum));
            var method = typeof(SerializationManager)
                .GetMethod(nameof(ValidateEnum), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(DataNode)])!
                .MakeGenericMethod(type);
            return (node, _) => (ValidationNode)method.Invoke(serialization, [node])!;
        }

        if (type.IsAssignableTo(typeof(ISelfSerialize)))
        {
            if (nodeType.IsAssignableTo(typeof(ValueDataNode)))
                return (node, _) => new ValidatedValueNode(node);

            return (node, _) => new ErrorNode(node, "Invalid nodetype for ISelfSerialize");
        }

        if (serialization.TryGetDefinition(type, out var dataDefinition))
        {
            var method = typeof(SerializationManager)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(m => m.Name == nameof(ValidateDataDefinition))
                .MakeGenericMethod(type);
            return (node, context) => (ValidationNode)method.Invoke(serialization, [node, dataDefinition, context])!;
        }
        else
        {
            var method = typeof(SerializationManager)
                .GetMethod(nameof(ValidateGenericValue),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    [
                        typeof(DataNode), typeof(ISerializationContext)
                    ])!
                .MakeGenericMethod(type, nodeType);
            return (node, context) => (ValidationNode)method.Invoke(serialization, [node, context])!;
        }
    }

    private ValidationNode ValidateArray<TElem>(SequenceDataNode sequenceDataNode, ISerializationContext? context)
    {
        var validatedList = new List<ValidationNode>();
        foreach (var dataNode in sequenceDataNode.Sequence)
        {
            validatedList.Add(ValidateNode<TElem>(dataNode, context));
        }

        return new ValidatedSequenceNode(validatedList);

    }

    private ValidationNode ValidateEnum<T>(DataNode node)
    {
        var enumName = node switch
        {
            ValueDataNode valueNode => valueNode.Value,
            SequenceDataNode sequenceNode => string.Join(", ", sequenceNode.Sequence),
            _ => null
        };

        if (enumName == null)
        {
            return new ErrorNode(node, $"Invalid node type {node.GetType()} for enum {typeof(T)}.");
        }

        if (!Enum.TryParse(typeof(T), enumName, true, out var enumValue))
        {
            return new ErrorNode(node, $"{enumValue} is not a valid enum value of type {typeof(T)}", false);
        }

        return new ValidatedValueNode(node);
    }

    private ValidationNode ValidateDataDefinition<T>(DataNode node, DataDefinition<T> dataDefinition, ISerializationContext? context) where T : notnull, ISerializationGenerated<T>
    {
        return node switch
        {
            ValueDataNode valueDataNode => valueDataNode.Value == ""
                ? new ValidatedValueNode(valueDataNode)
                : new ErrorNode(node, "Invalid NodeType for DataDefinition", false),
            MappingDataNode mappingDataNode => dataDefinition.Validate(this, mappingDataNode, context),
            _ => new ErrorNode(node, "Invalid NodeType for DataDefinition")
        };
    }

    private ValidationNode ValidateGenericValue<T, TNode>(DataNode node, ISerializationContext? context)
        where T : notnull
        where TNode : DataNode
    {
        if (context != null
            && context.SerializerProvider.TryGetTypeNodeSerializer<ITypeValidator<T, TNode>, T, TNode>(out var seri))
        {
            return seri.Validate(this, (TNode)node, DependencyCollection, context);
        }

        throw new Exception($"Failed to get node validator. Type: {typeof(T).Name}. Node type: {node.GetType().Name}. Yaml:\n{node}");
    }

    #endregion

    public ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null)
    {
        return ValidateNode(typeof(T), node, context);
    }

    public ValidationNode ValidateNode<T, TNode>(ITypeValidator<T, TNode> typeValidator, TNode node,
        ISerializationContext? context = null) where TNode : DataNode
    {
        return typeValidator.Validate(this, node, DependencyCollection, context);
    }

    public ValidationNode ValidateNode<T, TNode, TValidator>(TNode node,
        ISerializationContext? context = null) where TNode : DataNode where TValidator : ITypeValidator<T, TNode>
    {
        return ValidateNode(GetOrCreateCustomTypeSerializer<TValidator>(), node, context);
    }

    public ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null)
    {
        var underlyingType = type.GetUnderlyingType();

        if (underlyingType != null) // implies that type was nullable
        {
            if (IsNull(node))
                return new ValidatedValueNode(node);
        }
        else
        {
            underlyingType = type;
        }

        if (node.Tag?.StartsWith("!type:") == true)
        {
            var typeString = node.Tag.Substring(6);
            if (!TryResolveConcreteType(underlyingType, typeString, out underlyingType))
            {
                return new ErrorNode(node, $"Failed to resolve !type tag: {typeString}", false);
            }
        }

        return GetOrCreateValidationDelegate(underlyingType, node.GetType())(node, context);
    }
}
