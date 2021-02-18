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
    public class ListSerializers<T> :
        ITypeSerializer<List<T>, SequenceDataNode>,
        ITypeSerializer<IReadOnlyList<T>, SequenceDataNode>,
        ITypeSerializer<IReadOnlyCollection<T>, SequenceDataNode>,
        ITypeSerializer<ImmutableList<T>, SequenceDataNode>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        public List<T> Read(SequenceDataNode node, ISerializationContext? context = null)
        {
            var list = new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(_serv3Manager.ReadValue<T>(dataNode, context));
            }

            return list;
        }

        public DataNode Write(ImmutableList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(value.ToList(), alwaysWrite, context);
        }

        public DataNode Write(List<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();
            foreach (var elem in value)
            {
                sequence.Add(_serv3Manager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        public DataNode Write(IReadOnlyCollection<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(value.ToList(), alwaysWrite, context);
        }

        public DataNode Write(IReadOnlyList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(value.ToList(), alwaysWrite, context);
        }

        IReadOnlyList<T> ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read(node, context);
        }

        IReadOnlyCollection<T> ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read(node, context);
        }

        ImmutableList<T> ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read(node, context).ToImmutableList();
        }
    }
}
