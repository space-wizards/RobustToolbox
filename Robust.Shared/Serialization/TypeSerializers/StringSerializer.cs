using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class StringSerializer : ITypeSerializer<string>
    {
        public string NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return valueDataNode.GetValue();
        }

        public DataNode TypeToNode(string value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value);
        }
    }
}
