using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition<T>
    {
        public delegate void PopulateDelegateSignature(
            ref T target,
            MappingDataNode mappingDataNode,
            ISerializationContext? context,
            bool skipHook);

        public delegate MappingDataNode SerializeDelegateSignature(
            T obj,
            ISerializationContext? context,
            bool alwaysWrite);

        public delegate void CopyDelegateSignature(
            T source,
            ref T target,
            ISerializationContext? context,
            bool skipHook);
    }
}
