using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public sealed partial class PrototypeIdListSerializer<T> : ITypeSerializer<IReadOnlyList<string>, SequenceDataNode>
        where T : class, IPrototype
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

        IReadOnlyList<string> ITypeReader<IReadOnlyList<string>, SequenceDataNode>.Read(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context, IReadOnlyList<string>? rawValue)
        {
            if(rawValue != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(IReadOnlyList<string>)}. Ignoring...");

            var list = new List<string>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(_prototypeSerializer.Read(
                    serializationManager,
                    (ValueDataNode) dataNode,
                    dependencies,
                    skipHook,
                    context));
            }

            return list;
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
