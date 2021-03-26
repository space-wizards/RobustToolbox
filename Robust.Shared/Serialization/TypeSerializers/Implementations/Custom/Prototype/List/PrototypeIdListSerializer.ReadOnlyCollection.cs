using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public partial class PrototypeIdListSerializer<T> :
        ITypeSerializer<IReadOnlyCollection<string>, SequenceDataNode>
        where T : IPrototype
    {
        ValidationNode ITypeValidator<IReadOnlyCollection<string>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return ValidateInternal(serializationManager, node, dependencies, context);
        }

        DeserializationResult ITypeReader<IReadOnlyCollection<string>, SequenceDataNode>.Read(
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
                var result = _prototypeSerializer.Read(
                    serializationManager,
                    (ValueDataNode) dataNode,
                    dependencies,
                    skipHook,
                    context);

                list.Add((string) result.RawValue!);
                mappings.Add(result);
            }

            return new DeserializedCollection<List<string>, string>(list, mappings,
                elements => new List<string>(elements));
        }

        DataNode ITypeWriter<IReadOnlyCollection<string>>.Write(
            ISerializationManager serializationManager,
            IReadOnlyCollection<string> value,
            bool alwaysWrite,
            ISerializationContext? context)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        IReadOnlyCollection<string> ITypeCopier<IReadOnlyCollection<string>>.Copy(
            ISerializationManager serializationManager,
            IReadOnlyCollection<string> source,
            IReadOnlyCollection<string> target,
            bool skipHook,
            ISerializationContext? context)
        {
            return new List<string>(source);
        }
    }
}
