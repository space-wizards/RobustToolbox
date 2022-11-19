using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary
{

    public sealed class AbstractPrototypeIdValueDictionarySerializer<TValue, TPrototype> : PrototypeIdValueDictionarySerializer<TValue,
            TPrototype> where TPrototype : class, IPrototype, IInheritingPrototype where TValue : notnull
    {
        protected override PrototypeIdSerializer<TPrototype> PrototypeSerializer =>
            new AbstractPrototypeIdSerializer<TPrototype>();
    }

    [Virtual]
    public class PrototypeIdValueDictionarySerializer<TValue, TPrototype> :
        ITypeSerializer<Dictionary<TValue, string>, MappingDataNode>,
        ITypeSerializer<SortedDictionary<TValue, string>, MappingDataNode>,
        ITypeSerializer<IReadOnlyDictionary<TValue, string>, MappingDataNode>
        where TPrototype : class, IPrototype where TValue : notnull
    {
        private readonly DictionarySerializer<TValue, string> _dictionarySerializer = new();
        protected virtual PrototypeIdSerializer<TPrototype> PrototypeSerializer => new();

        private ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            var mapping = new Dictionary<ValidationNode, ValidationNode>();

            foreach (var (key, val) in node.Children)
            {
                if (val is not ValueDataNode value)
                {
                    mapping.Add(new ErrorNode(val, $"Cannot cast node {val} to ValueDataNode."), serializationManager.ValidateNode(typeof(TValue), key, context));
                    continue;
                }

                mapping.Add(PrototypeSerializer.Validate(serializationManager, value, dependencies, context), serializationManager.ValidateNode(typeof(TValue), key, context));
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

        Dictionary<TValue, string> ITypeReader<Dictionary<TValue, string>, MappingDataNode>.Read(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context,
            Dictionary<TValue, string>? value)
        {
            return _dictionarySerializer.Read(serializationManager, node, dependencies, skipHook, context, value);
        }

        SortedDictionary<TValue, string> ITypeReader<SortedDictionary<TValue, string>, MappingDataNode>.Read(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context,
            SortedDictionary<TValue, string>? value)
        {
            return ((ITypeReader<SortedDictionary<TValue, string>, MappingDataNode>)_dictionarySerializer).Read(serializationManager, node, dependencies, skipHook, context, value);
        }

        IReadOnlyDictionary<TValue, string> ITypeReader<IReadOnlyDictionary<TValue, string>, MappingDataNode>.Read(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context,
            IReadOnlyDictionary<TValue, string>? value)
        {
            return ((ITypeReader<IReadOnlyDictionary<TValue, string>, MappingDataNode>)_dictionarySerializer).Read(serializationManager, node, dependencies, skipHook, context, value);
        }

        public DataNode Write(ISerializationManager serializationManager, Dictionary<TValue, string> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return _dictionarySerializer.Write(serializationManager, value, dependencies, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, SortedDictionary<TValue, string> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return _dictionarySerializer.Write(serializationManager, value, dependencies, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyDictionary<TValue, string> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return _dictionarySerializer.Write(serializationManager, value, dependencies, alwaysWrite, context);
        }

        public Dictionary<TValue, string> Copy(ISerializationManager serializationManager,
            Dictionary<TValue, string> source, Dictionary<TValue, string> target, bool skipHook,
            ISerializationContext? context = null)
        {
            return _dictionarySerializer.Copy(serializationManager, source, target, skipHook, context);
        }

        public SortedDictionary<TValue, string> Copy(ISerializationManager serializationManager,
            SortedDictionary<TValue, string> source, SortedDictionary<TValue, string> target,
            bool skipHook, ISerializationContext? context = null)
        {
            return _dictionarySerializer.Copy(serializationManager, source, target, skipHook, context);
        }

        public IReadOnlyDictionary<TValue, string> Copy(ISerializationManager serializationManager,
            IReadOnlyDictionary<TValue, string> source,
            IReadOnlyDictionary<TValue, string> target, bool skipHook, ISerializationContext? context = null)
        {
            return _dictionarySerializer.Copy(serializationManager, source, target, skipHook, context);
        }
    }
}
