using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.YAML;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class DictionarySerializer<TKey, TValue> : ITypeSerializer<Dictionary<TKey, TValue>>, ITypeSerializer<IReadOnlyDictionary<TKey, TValue>>, ITypeSerializer<SortedDictionary<TKey, TValue>> where TKey : notnull
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        private DataNode InterfaceTypeToNode(IDictionary<TKey, TValue> value, IDataNodeFactory nodeFactory,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mappingNode = nodeFactory.GetMappingNode();
            foreach (var (key, val) in value)
            {
                mappingNode.AddNode(_serv3Manager.WriteValue<TKey>(key, nodeFactory, alwaysWrite, context),
                    _serv3Manager.WriteValue<TValue>(val, nodeFactory, alwaysWrite, context));
            }

            return mappingNode;
        }

        private Dictionary<TKey, TValue> NormalNodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not MappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            var dict = new Dictionary<TKey, TValue>();
            foreach (var (key, value) in mappingDataNode.Children)
            {
                var keyValue = _serv3Manager.ReadValue<TKey>(key, context);

                var valueValue = _serv3Manager.ReadValue<TValue>(value, context);
                dict.Add(keyValue, valueValue);
            }

            return dict;
        }

        Dictionary<TKey, TValue> ITypeSerializer<Dictionary<TKey, TValue>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return NormalNodeToType(node, context);
        }

        public DataNode TypeToNode(Dictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceTypeToNode(value, nodeFactory, alwaysWrite, context);
        }

        public DataNode TypeToNode(SortedDictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceTypeToNode(value, nodeFactory, alwaysWrite, context);
        }

        public DataNode TypeToNode(IReadOnlyDictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceTypeToNode(value.ToDictionary(k => k.Key, v => v.Value), nodeFactory, alwaysWrite, context);
        }

        IReadOnlyDictionary<TKey, TValue> ITypeSerializer<IReadOnlyDictionary<TKey, TValue>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return NormalNodeToType(node, context);
        }

        SortedDictionary<TKey, TValue> ITypeSerializer<SortedDictionary<TKey, TValue>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return new(NormalNodeToType(node, context));
        }
    }
}
