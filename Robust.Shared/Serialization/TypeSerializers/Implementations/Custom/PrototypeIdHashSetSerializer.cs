using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom
{
    public class PrototypeIdHashSetSerializer<TPrototype> : ITypeSerializer<HashSet<string>, SequenceDataNode> where TPrototype : IPrototype
    {
        public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            var protoMan = dependencies.Resolve<IPrototypeManager>();
            var list = new List<ValidationNode>();

            foreach (var dataNode in node.Sequence)
            {
                var value = (ValueDataNode) dataNode;
                list.Add(protoMan.HasIndex<TPrototype>(value.Value)
                    ? new ValidatedValueNode(value)
                    : new ErrorNode(value, $"PrototypeID {value.Value} for type {typeof(TPrototype)} not found"));
            }

            return new ValidatedSequenceNode(list);
        }

        public DeserializationResult Read(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            var set = new HashSet<string>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<string>(dataNode, context, skipHook);

                set.Add(value);
                mappings.Add(result);
            }

            return new DeserializedCollection<HashSet<string>, string>(set, mappings,
                elements => new HashSet<string>(elements));
        }

        public DataNode Write(ISerializationManager serializationManager, HashSet<string> value, bool alwaysWrite = false,
                ISerializationContext? context = null)
            {
                var list = new List<DataNode>();

                foreach (var str in value)
                {
                    list.Add(new ValueDataNode(str));
                }

                return new SequenceDataNode(list);
            }

            public HashSet<string> Copy(ISerializationManager serializationManager, HashSet<string> source, HashSet<string> target, bool skipHook,
                ISerializationContext? context = null)
            {
                return new (source);
            }
    }
}
