using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Constraints.Interfaces;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public partial class SerializationManager
{
    public ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null)
    {
        if (IsNull(node))
        {
            return type.IsNullable()
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Null value for non-nullable type");
        }

        var underlyingType = type.EnsureNotNullableType();

        if (underlyingType.IsArray)
        {
            if (node is not SequenceDataNode sequenceDataNode) return new ErrorNode(node, "Invalid nodetype for array.", true);
            var elementType = underlyingType.GetElementType();
            if (elementType == null)
                throw new ArgumentException($"Failed to get elementtype of arraytype {underlyingType}", nameof(underlyingType));
            var validatedList = new List<ValidationNode>();
            foreach (var dataNode in sequenceDataNode.Sequence)
            {
                validatedList.Add(ValidateNode(elementType, dataNode, context));
            }

            return new ValidatedSequenceNode(validatedList);
        }

        if (underlyingType.IsEnum)
        {
            var enumName = node switch
            {
                ValueDataNode valueNode => valueNode.Value,
                SequenceDataNode sequenceNode => string.Join(", ", sequenceNode.Sequence),
                _ => null
            };

            if (enumName == null)
            {
                return new ErrorNode(node, $"Invalid node type {node.GetType().Name} for enum {underlyingType}.");
            }

            if (!Enum.TryParse(underlyingType, enumName, true, out var enumValue))
            {
                return new ErrorNode(node, $"{enumValue} is not a valid enum value of type {underlyingType}", false);
            }

            return new ValidatedValueNode(node);
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

        if (TryValidateWithTypeValidator(underlyingType, node, DependencyCollection, context, out var valid)) return valid;

        if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            return node is ValueDataNode valueDataNode ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for ISelfSerialize", true);

        if (TryGetDefinition(underlyingType, out var dataDefinition))
        {
            return node switch
            {
                ValueDataNode valueDataNode => valueDataNode.Value == "" ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for Datadefinition", false),
                MappingDataNode mappingDataNode => dataDefinition.Validate(this, mappingDataNode, context, DependencyCollection),
                _ => new ErrorNode(node, "Invalid nodetype for Datadefinition", true)
            };
        }

        return new ErrorNode(node, "Failed to read node.", false);
    }

    public ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null)
    {
        return ValidateNode(typeof(T), node, context);
    }

    public ValidationNode ValidateNodeWith(Type type, Type typeSerializer, DataNode node,
        ISerializationContext? context = null)
    {
        var method =
            typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(ValidateWithSerializer))!.MakeGenericMethod(
                type, node.GetType(), typeSerializer);
        return (ValidationNode)method.Invoke(this, new object?[] {node, context})!;
    }

    public ValidationNode ValidateNodeWith<TType, TSerializer, TNode>(TNode node,
        ISerializationContext? context = null)
        where TSerializer : ITypeValidator<TType, TNode>
        where TNode: DataNode
    {
        return ValidateNodeWith(typeof(TType), typeof(TSerializer), node, context);
    }
}
