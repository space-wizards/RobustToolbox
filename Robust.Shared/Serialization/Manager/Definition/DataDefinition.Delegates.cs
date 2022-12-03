using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager.Definition
{
    internal partial class DataDefinition<T>
    {
        //todo paul make these use the mngr delegates
        public delegate void PopulateDelegateSignature(
            ref T target,
            MappingDataNode mappingDataNode,
            SerializationHookContext hookCtx,
            ISerializationContext? context);

        public delegate MappingDataNode SerializeDelegateSignature(
            T obj,
            ISerializationContext? context,
            bool alwaysWrite);

        public delegate void CopyDelegateSignature(
            T source,
            ref T target,
            SerializationHookContext hookCtx,
            ISerializationContext? context);

        private delegate ValidationNode ValidateFieldDelegate(
            DataNode node,
            ISerializationContext? context);
    }
}
