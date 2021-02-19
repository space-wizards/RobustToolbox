using System.Text.RegularExpressions;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class RegexSerializer : ITypeSerializer<Regex, ValueDataNode>
    {
        public Regex Read(ValueDataNode node, ISerializationContext? context = null)
        {
            return new(node.Value, RegexOptions.Compiled);
        }

        public DataNode Write(Regex value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }
    }
}
