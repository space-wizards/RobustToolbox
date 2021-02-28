using System;
using System.Linq;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class ValueTupleSerializer<T1, T2> : ITypeSerializer<ValueTuple<T1, T2>, MappingDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            ISerializationContext? context = null)
        {
            if (node.Children.Count != 1)
                throw new InvalidMappingException("Less than or more than 1 mappings provided to ValueTupleSerializer");

            var entry = node.Children.First();
            var v1 = serializationManager.ReadValueOrThrow<T1>(entry.Key, context);
            var v2 = serializationManager.ReadValueOrThrow<T2>(entry.Value, context);

            return DeserializationResult.Value(new ValueTuple<T1, T2>(v1, v2));
        }

        public DataNode Write(ISerializationManager serializationManager, (T1, T2) value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mapping = new MappingDataNode();
            mapping.AddNode(
                serializationManager.WriteValue(value.Item1, alwaysWrite, context),
                serializationManager.WriteValue(value.Item2, alwaysWrite, context)
            );

            return mapping;
        }

        public (T1, T2) Copy(ISerializationManager serializationManager, (T1, T2) source, (T1, T2) target,
            ISerializationContext? context = null)
        {
            return (serializationManager.Copy(source.Item1, target.Item1)!,
                serializationManager.Copy(source.Item2, source.Item2)!);
        }
    }
}
