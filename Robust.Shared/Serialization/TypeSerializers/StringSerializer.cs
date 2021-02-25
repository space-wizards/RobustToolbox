using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class StringSerializer : ITypeSerializer<string, ValueDataNode>
    {
        public string Read(ValueDataNode node, ISerializationContext? context = null)
        {
            return node.Value;
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
