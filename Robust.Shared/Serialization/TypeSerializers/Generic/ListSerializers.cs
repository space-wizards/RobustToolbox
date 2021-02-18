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
    public class ListSerializers<T> : ITypeSerializer<List<T>>, ITypeSerializer<IReadOnlyList<T>>, ITypeSerializer<IReadOnlyCollection<T>>, ITypeSerializer<ImmutableList<T>>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        public List<T> NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not SequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();

            var list = new List<T>();
            foreach (var dataNode in sequenceDataNode.Sequence)
            {
                list.Add(_serv3Manager.ReadValue<T>(dataNode, context));
            }

            return list;
        }

        public DataNode TypeToNode(ImmutableList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToList(), alwaysWrite, context);
        }

        public DataNode TypeToNode(List<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();
            foreach (var elem in value)
            {
                sequence.Add(_serv3Manager.WriteValue(elem, nodeFactory, alwaysWrite, context));
            }

            return sequence;
        }

        public DataNode TypeToNode(IReadOnlyCollection<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToList(), alwaysWrite, context);
        }

        public DataNode TypeToNode(IReadOnlyList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToList(), alwaysWrite, context);
        }

        IReadOnlyList<T> ITypeSerializer<IReadOnlyList<T>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return NodeToType(node, context);
        }

        IReadOnlyCollection<T> ITypeSerializer<IReadOnlyCollection<T>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return NodeToType(node, context);
        }

        ImmutableList<T> ITypeSerializer<ImmutableList<T>>.NodeToType(DataNode node, ISerializationContext? context)
        {
            return NodeToType(node, context).ToImmutableList();
        }
    }
}
