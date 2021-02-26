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
        public DeserializationResult<string> Read(ValueDataNode node, ISerializationContext? context = null)
        {
            return new DeserializedValue<string>(node.Value);
        }

        public DataNode Write(string value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value);
        }

        [MustUseReturnValue]
        public string Copy(string source, string target)
        {
            return source;
        }
    }
}
