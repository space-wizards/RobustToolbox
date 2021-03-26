using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public partial class PrototypeIdListSerializer<T> : ITypeSerializer<IReadOnlyList<string>, SequenceDataNode>
        where T : IPrototype
    {
        DataNode ITypeWriter<IReadOnlyList<string>>.Write(
            ISerializationManager serializationManager,
            IReadOnlyList<string> value,
            bool alwaysWrite,
            ISerializationContext? context)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        [MustUseReturnValue]
        IReadOnlyList<string> ITypeCopier<IReadOnlyList<string>>.Copy(
            ISerializationManager serializationManager,
            IReadOnlyList<string> source,
            IReadOnlyList<string> target,
            bool skipHook,
            ISerializationContext? context)
        {
            return new List<string>(source);
        }

        DeserializationResult ITypeReader<IReadOnlyList<string>, SequenceDataNode>.Read(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context)
        {
            var list = new List<string>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var result = _prototypeSerializer.ReadInternal(
                    serializationManager,
                    (ValueDataNode) dataNode,
                    dependencies,
                    skipHook,
                    context);

                list.Add(result.Value);
                mappings.Add(result);
            }

            return new DeserializedCollection<IReadOnlyList<string>, string>(list, mappings,
                elements => new List<string>(elements));
        }

        ValidationNode ITypeValidator<IReadOnlyList<string>, SequenceDataNode>.Validate(
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

                list.Add(_prototypeSerializer.Validate(serializationManager, value, dependencies, context));
            }

            return new ValidatedSequenceNode(list);
        }
    }
}
