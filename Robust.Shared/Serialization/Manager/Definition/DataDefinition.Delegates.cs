using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition
    {
        private delegate object PopulateDelegateSignature(
            object target,
            MappingDataNode mappingDataNode,
            ISerializationManager serializationManager,
            ISerializationContext? context,
            bool skipHook,
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

        private delegate TValue AccessField<TTarget, TValue>(ref TTarget target);

        private delegate void AssignField<TTarget, TValue>(ref TTarget target, TValue? value);
    }
}
