using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class DictionarySerializer<TKey, TValue> :
        ITypeSerializer<Dictionary<TKey, TValue>, MappingDataNode>,
        ITypeSerializer<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>,
        ITypeSerializer<SortedDictionary<TKey, TValue>, MappingDataNode> where TKey : notnull
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        private MappingDataNode InterfaceWrite(
            IDictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mappingNode = new MappingDataNode();

            foreach (var (key, val) in value)
            {
                mappingNode.AddNode(
                    _serv3Manager.WriteValue(key, alwaysWrite, context),
                    _serv3Manager.WriteValue(val, alwaysWrite, context));
            }

            return mappingNode;
        }

        private Dictionary<TKey, TValue> NormalRead(MappingDataNode node, ISerializationContext? context = null)
        {
            var dict = new Dictionary<TKey, TValue>();

            foreach (var (key, value) in node.Children)
            {
                var keyValue = _serv3Manager.ReadValue<TKey>(key, context);

                var valueValue = _serv3Manager.ReadValue<TValue>(value, context);
                dict.Add(keyValue, valueValue);
            }

            return dict;
        }

        public Dictionary<TKey, TValue> Read(MappingDataNode node, ISerializationContext? context)
        {
            return NormalRead(node, context);
        }

        public DataNode Write(Dictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(value, alwaysWrite, context);
        }

        public DataNode Write(SortedDictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(value, alwaysWrite, context);
        }

        public DataNode Write(IReadOnlyDictionary<TKey, TValue> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return InterfaceWrite(value.ToDictionary(k => k.Key, v => v.Value), alwaysWrite, context);
        }

        IReadOnlyDictionary<TKey, TValue> ITypeReader<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Read(MappingDataNode node, ISerializationContext? context)
        {
            return NormalRead(node, context);
        }

        SortedDictionary<TKey, TValue> ITypeReader<SortedDictionary<TKey, TValue>, MappingDataNode>.Read(MappingDataNode node, ISerializationContext? context)
        {
            return new(NormalRead(node, context));
        }
    }
}
