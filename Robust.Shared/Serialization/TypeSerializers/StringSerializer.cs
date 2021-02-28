using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class StringSerializer : ITypeSerializer<string, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return new DeserializedValue<string>(node.Value);
        }

        public DataNode Write(ISerializationManager serializationManager, string value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value);
        }

        [MustUseReturnValue]
        public string Copy(ISerializationManager serializationManager, string source, string target, ISerializationContext? context = null)
        {
            return source;
        }
    }
}
