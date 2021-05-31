using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition
    {
        private delegate DeserializedFieldEntry[] DeserializeDelegate(
            object obj,
            MappingDataNode mappingDataNode,
            ISerializationManager serializationManager,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            bool skipHook);

        private delegate DeserializationResult PopulateDelegateSignature(
            object target,
            DeserializedFieldEntry[] deserializationResults,
            object?[] defaultValues);

        private delegate MappingDataNode SerializeDelegateSignature(
            object obj,
            ISerializationManager serializationManager,
            ISerializationContext? context,
            bool alwaysWrite,
            object?[] defaultValues);

        private delegate object CopyDelegateSignature(
            object source,
            object target,
            ISerializationManager serializationManager,
            ISerializationContext? context);

        private delegate DeserializationResult CreateDefinitionDelegate(
            object value,
            DeserializedFieldEntry[] mappings);

        private delegate TValue AccessField<TTarget, TValue>(ref TTarget target);

        private delegate void AssignField<TTarget, TValue>(ref TTarget target, TValue? value);

    }
}
