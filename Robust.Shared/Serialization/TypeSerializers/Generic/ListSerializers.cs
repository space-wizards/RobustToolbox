using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    public class ListSerializers<T> : ITypeSerializer<List<T>>, ITypeSerializer<IReadOnlyList<T>>, ITypeSerializer<IReadOnlyCollection<T>>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        public List<T> NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not ISequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();

            var list = new List<T>();
            foreach (var dataNode in sequenceDataNode.Sequence)
            {
                list.Add(_serv3Manager.ReadValue<T>(dataNode, context));
            }

            return list;
        }

        public IDataNode TypeToNode(List<T> value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = nodeFactory.GetSequenceNode();
            foreach (var elem in value)
            {
                sequence.Add(_serv3Manager.WriteValue(elem, nodeFactory, alwaysWrite, context));
            }

            return sequence;
        }

        public IDataNode TypeToNode(IReadOnlyCollection<T> value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToList(), nodeFactory, alwaysWrite, context);
        }

        public IDataNode TypeToNode(IReadOnlyList<T> value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return TypeToNode(value.ToList(), nodeFactory, alwaysWrite, context);
        }

        IReadOnlyList<T> ITypeSerializer<IReadOnlyList<T>>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            return NodeToType(node, context);
        }

        IReadOnlyCollection<T> ITypeSerializer<IReadOnlyCollection<T>>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            return NodeToType(node, context);
        }
    }
}
