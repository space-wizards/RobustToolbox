using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary
{

    public sealed class AbstractPrototypeIdDictionarySerializer<TValue, TPrototype> : PrototypeIdDictionarySerializer<TValue,
            TPrototype> where TPrototype : class, IPrototype, IInheritingPrototype
    {
        protected override PrototypeIdSerializer<TPrototype> PrototypeSerializer =>
            new AbstractPrototypeIdSerializer<TPrototype>();
    }

    [Virtual]
    public class PrototypeIdDictionarySerializer<TValue, TPrototype> :
        ITypeValidator<Dictionary<string, TValue>, MappingDataNode>,
        ITypeValidator<SortedDictionary<string, TValue>, MappingDataNode>,
        ITypeValidator<IReadOnlyDictionary<string, TValue>, MappingDataNode>
        where TPrototype : class, IPrototype
    {
        protected virtual PrototypeIdSerializer<TPrototype> PrototypeSerializer => new();

        private ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            var mapping = new Dictionary<ValidationNode, ValidationNode>();

            foreach (var (key, val) in node.Children)
            {
                var keyNode = new ValueDataNode(key);
                mapping.Add(PrototypeSerializer.Validate(serializationManager, keyNode, dependencies, context), serializationManager.ValidateNode<TValue>(val, context));
            }

            return new ValidatedMappingNode(mapping);
        }

        ValidationNode ITypeValidator<Dictionary<string, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<SortedDictionary<string, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<IReadOnlyDictionary<string, TValue>, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, dependencies, context);
        }
    }
}
