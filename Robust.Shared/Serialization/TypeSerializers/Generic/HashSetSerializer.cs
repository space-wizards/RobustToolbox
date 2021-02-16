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
    public class HashSetSerializer<T> : ITypeSerializer<HashSet<T>>, ITypeSerializer<ImmutableHashSet<T>>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        HashSet<T> NormalNodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not ISequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();
            var hashset = new HashSet<T>();
            foreach (var dataNode in sequenceDataNode.Sequence)
            {
                hashset.Add(_serv3Manager.ReadValue<T>(dataNode, context));
            }

            return hashset;
        }

        HashSet<T> ITypeSerializer<HashSet<T>>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            return NormalNodeToType(node, context);
        }

        public IDataNode TypeToNode(ImmutableHashSet<T> value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToHashSet(), nodeFactory, alwaysWrite, context);
        }

        public IDataNode TypeToNode(HashSet<T> value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = nodeFactory.GetSequenceNode();
            foreach (var elem in value)
            {
                sequence.Add(_serv3Manager.WriteValue(elem, nodeFactory, alwaysWrite, context));
            }

            return sequence;
        }

        ImmutableHashSet<T> ITypeSerializer<ImmutableHashSet<T>>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            return NormalNodeToType(node, context).ToImmutableHashSet();
        }
    }
}
