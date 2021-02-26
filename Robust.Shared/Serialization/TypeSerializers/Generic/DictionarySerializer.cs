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
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private MappingDataNode InterfaceWrite(
            IDictionary<TKey, TValue> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mappingNode = new MappingDataNode();

            foreach (var (key, val) in value)
            {
                mappingNode.AddNode(
                    _serializationManager.WriteValue(key, alwaysWrite, context),
                    _serializationManager.WriteValue(val, alwaysWrite, context));
            }

            return mappingNode;
        }

        private DeserializationResult NormalRead(MappingDataNode node, ISerializationContext? context = null)
        {
            var dict = new Dictionary<TKey, TValue>();
            var mappedFields = new Dictionary<DeserializationResult, DeserializationResult>();

            var i = 0;
            foreach (var (key, value) in node.Children)
            {
                var keyRes = _serializationManager.ReadValue<TKey>(key, context);

                var valueRes = _serializationManager.ReadValue<TValue>(value, context);
                dict.Add((TKey)keyRes.GetValue()!, (TValue)valueRes.GetValue()!);
                mappedFields.Add(keyRes, valueRes);
            }

            return new DeserializedDictionary<TKey, TValue>(dict, mappedFields);
        }

        public DeserializationResult Read(MappingDataNode node, ISerializationContext? context)
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

        DeserializationResult ITypeReader<IReadOnlyDictionary<TKey, TValue>, MappingDataNode>.Read(MappingDataNode node, ISerializationContext? context)
        {
            return NormalRead(node, context);
        }

        DeserializationResult ITypeReader<SortedDictionary<TKey, TValue>, MappingDataNode>.Read(MappingDataNode node, ISerializationContext? context)
        {
            var res = (DeserializedDictionary<TKey, TValue>)NormalRead(node, context);
            return res.WithDict(new SortedDictionary<TKey, TValue>(res.Value));
        }
    }
}
