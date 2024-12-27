using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
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

    private ValidationDelegate GetOrCreateValidationDelegate(Type type, Type node)
    {
        return _validationDelegates.GetOrAdd((type, node), static (key, manager) =>
        {
            var managerConst = Expression.Constant(manager);
            var nodeParam = Expression.Parameter(typeof(DataNode), "node");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            Expression call;
            if (manager._regularSerializerProvider.TryGetTypeNodeSerializer(typeof(ITypeValidator<,>), key.type, key.node, out var serializer))
            {
                var serializerConst = Expression.Constant(serializer);

                call = Expression.Call(
                    managerConst,
                    nameof(ValidateNode),
                    new []{key.type, key.node},
                    serializerConst,
                    Expression.Convert(nodeParam, key.node),
                    contextParam);
            }
            else if (key.type.IsArray)
            {
                if (!key.node.IsAssignableTo(typeof(SequenceDataNode)))
                {
                    call = manager.ErrorNodeExpression(nodeParam, "Invalid nodetype for array.", true);
                }
                else
                {
                    var elementType = key.type.GetElementType();
                    if (elementType == null)
                        throw new ArgumentException($"Failed to get ElementType of ArrayType {key.type}");

                    call = Expression.Call(
                        managerConst,
                        nameof(ValidateArray),
                        new[] { elementType },
                        Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                        contextParam);
                }
            }
            else if (key.type.IsEnum)
            {
                // Does not include cases where the target type is System.Enum.
                // Those get handled by the generic enum serializer which uses reflection to resolve strings into enums.
                DebugTools.Assert(key.type != typeof(Enum));

                call = Expression.Call(
                    managerConst,
                    nameof(ValidateEnum),
                    new[] { key.type },
                    nodeParam);
            }
            else if (key.type.IsAssignableTo(typeof(ISelfSerialize)))
            {
                if (key.node.IsAssignableTo(typeof(ValueDataNode)))
                {
                    call = manager.ValidateNodeExpression(nodeParam);
                }
                else
                {
                    call = manager.ErrorNodeExpression(nodeParam, "Invalid nodetype for ISelfSerialize");
                }
            }
            else if (manager.TryGetDefinition(key.type, out var dataDefinition))
            {
                var dataDefConst = Expression.Constant(dataDefinition, typeof(DataDefinition<>).MakeGenericType(key.type));

                call = Expression.Call(
                    managerConst,
                    nameof(ValidateDataDefinition),
                    new []{key.type},
                    nodeParam,
                    dataDefConst,
                    contextParam);
            }
            else
            {
                call = Expression.Call(
                    managerConst,
                    nameof(ValidateGenericValue),
                    new[] { key.type, key.node },
                    nodeParam,
                    contextParam);
            }

            //insert a nullcheck at the beginning, but ONLY if we are actually found a way of validating this node
            call = Expression.Condition(
                Expression.Call(
                    typeof(SerializationManager),
                    nameof(IsNull),
                    Type.EmptyTypes,
                    nodeParam),
                Expression.Convert(key.type.IsNullable()
                    ? manager.ValidateNodeExpression(nodeParam)
                    : manager.ErrorNodeExpression(nodeParam, "Non-nullable field contained a null value", true), typeof(ValidationNode)),
                Expression.Convert(call, typeof(ValidationNode)));

            return Expression.Lambda<ValidationDelegate>(
                call,
                nodeParam,
                contextParam).Compile();
        }, this);
    }

    private Expression ErrorNodeExpression(ParameterExpression nodeParam, string message, bool alwaysRelevant = true)
    {
        return ExpressionUtils.NewExpression<ErrorNode>(nodeParam, message, alwaysRelevant);
    }

    private Expression ValidateNodeExpression(ParameterExpression nodeParam)
    {
        return ExpressionUtils.NewExpression<ValidatedValueNode>(nodeParam);
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

    private ValidationNode ValidateDataDefinition<T>(DataNode node, DataDefinition<T> dataDefinition, ISerializationContext? context) where T : notnull
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
            try
            {
                underlyingType = ResolveConcreteType(underlyingType, typeString);
            }
            catch (InvalidOperationException)
            {
                return new ErrorNode(node, $"Failed to resolve !type tag: {typeString}", false);
            }
        }

        return GetOrCreateValidationDelegate(underlyingType, node.GetType())(node, context);
    }
}
