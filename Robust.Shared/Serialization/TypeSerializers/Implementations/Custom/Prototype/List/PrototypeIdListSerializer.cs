using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public sealed class AbstractPrototypeIdListSerializer<T> : PrototypeIdListSerializer<T> where T : class, IPrototype, IInheritingPrototype
    {
        protected override PrototypeIdSerializer<T> PrototypeSerializer => new AbstractPrototypeIdSerializer<T>();
    }

    [Virtual]
    public partial class PrototypeIdListSerializer<T> : ITypeSerializer<List<string>, SequenceDataNode> where T : class, IPrototype
    {
        protected virtual PrototypeIdSerializer<T> PrototypeSerializer => new();

        private ValidationNode ValidateInternal(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            var list = new List<ValidationNode>();

            foreach (var dataNode in node.Sequence)
            {
                if (dataNode is not ValueDataNode value)
                {
                    list.Add(new ErrorNode(dataNode, $"Cannot cast node {dataNode} to ValueDataNode."));
                    continue;
                }

                list.Add(PrototypeSerializer.Validate(serializationManager, value, dependencies, context));
            }

            return new ValidatedSequenceNode(list);
        }

        private DataNode WriteInternal(
            ISerializationManager serializationManager,
            IEnumerable<string> value,
            IDependencyCollection dependencies,
            bool alwaysWrite,
            ISerializationContext? context)
        {
            var list = new List<DataNode>();

            foreach (var str in value)
            {
                list.Add(PrototypeSerializer.Write(serializationManager, str, dependencies, alwaysWrite, context));
            }

            return new SequenceDataNode(list);
        }

        ValidationNode ITypeValidator<List<string>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return ValidateInternal(serializationManager, node, dependencies, context);
        }

        List<string> ITypeReader<List<string>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context, List<string>? list)
        {
            list ??= new List<string>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(PrototypeSerializer.Read(
                    serializationManager,
                    (ValueDataNode) dataNode,
                    dependencies,
                    skipHook,
                    context));
            }

            return list;
        }

        DataNode ITypeWriter<List<string>>.Write(ISerializationManager serializationManager,
            List<string> value,
            IDependencyCollection dependencies,
            bool alwaysWrite,
            ISerializationContext? context)
        {
            return WriteInternal(serializationManager, value, dependencies, alwaysWrite, context);
        }

        List<string> ITypeCopier<List<string>>.Copy(
            ISerializationManager serializationManager,
            List<string> source,
            List<string> target,
            bool skipHook,
            ISerializationContext? context)
        {
            return new(source);
        }
    }
}
