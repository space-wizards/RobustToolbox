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
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        private HashSet<T> NormalRead(SequenceDataNode node, ISerializationContext? context)
        {
            var hashset = new HashSet<T>();

            foreach (var dataNode in node.Sequence)
            {
                hashset.Add(_serv3Manager.ReadValue<T>(dataNode, context));
            }

            return hashset;
        }

        HashSet<T> ITypeReader<HashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
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
                sequence.Add(_serv3Manager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        ImmutableHashSet<T> ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
            ISerializationContext? context)
        {
            return NormalRead(node, context).ToImmutableHashSet();
        }
    }
}
