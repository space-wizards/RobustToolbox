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
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        public DeserializationResult Read(SequenceDataNode node, ISerializationContext? context = null)
        {
            var list = new List<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var res = _serializationManager.ReadValue<T>(dataNode, context);
                list.Add((T)res.GetValue()!);
                results.Add(res);
            }

            return new DeserializedList<T>(list, results);
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
                sequence.Add(_serializationManager.WriteValue(elem, alwaysWrite, context));
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

        DeserializationResult ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read(node, context);
        }

        DeserializationResult ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read(node, context);
        }

        DeserializationResult ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            var res = (DeserializedList<T>)Read(node, context);
            return res.WithList(res.Value.ToImmutableList());
        }
    }
}
