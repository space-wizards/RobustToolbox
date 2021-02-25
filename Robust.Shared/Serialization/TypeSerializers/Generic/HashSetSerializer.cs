using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class HashSetSerializer<T> :
        ITypeSerializer<HashSet<T>, SequenceDataNode>,
        ITypeSerializer<ImmutableHashSet<T>, SequenceDataNode>
    {
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private DeserializationResult NormalRead(SequenceDataNode node, ISerializationContext? context)
        {
            var hashset = new HashSet<T>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var res = _serializationManager.ReadValue<T>(dataNode, context)
                hashset.Add((T) res.GetValue()!);
                mappings.Add(res);
            }

            return new DeserializedSet<T>(hashset, mappings);
        }

        DeserializationResult ITypeReader<HashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
            ISerializationContext? context)
        {
            return NormalRead(node, context);
        }

        public DataNode Write(ImmutableHashSet<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(value.ToHashSet(), alwaysWrite, context);
        }

        public DataNode Write(HashSet<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(_serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        DeserializationResult ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
            ISerializationContext? context)
        {
            var res = (DeserializedSet<T>)NormalRead(node, context);
            return res.WithSet(res.Value.ToImmutableHashSet());
        }
    }
}
