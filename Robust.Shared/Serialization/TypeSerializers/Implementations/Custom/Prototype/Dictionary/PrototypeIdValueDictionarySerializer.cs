using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary
{
    [Obsolete("Use ProtoId instead")]
    public sealed class PrototypeIdValueDictionarySerializer<TValue, TPrototype> :
        ITypeValidator<Dictionary<TValue, string>, MappingDataNode>,
        ITypeValidator<SortedDictionary<TValue, string>, MappingDataNode>,
        ITypeValidator<IReadOnlyDictionary<TValue, string>, MappingDataNode>
        where TPrototype : class, IPrototype where TValue : notnull
    {
        private ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            var mapping = new Dictionary<ValidationNode, ValidationNode>();

            foreach (var (k, val) in node.Children)
            {
                var key = node.GetKeyNode(k);
                if (val is not ValueDataNode value)
                {
                    mapping.Add(new ErrorNode(val, $"Cannot cast node {val} to ValueDataNode."), serializationManager.ValidateNode<TValue>(key, context));
                    continue;
                }

                mapping.Add(ProtoIdSerializer<TPrototype>.Validate(dependencies, value), serializationManager.ValidateNode<TValue>(key, context));
            }

            return new ValidatedMappingNode(mapping);
        }

        ValidationNode ITypeValidator<Dictionary<TValue, string>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<SortedDictionary<TValue, string>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<IReadOnlyDictionary<TValue, string>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }
    }
}
