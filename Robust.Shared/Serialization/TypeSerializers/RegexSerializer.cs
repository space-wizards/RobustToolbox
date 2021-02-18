using System.Text.RegularExpressions;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class RegexSerializer : ITypeSerializer<Regex>
    {
        public Regex NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueNode)
            {
                throw new InvalidNodeTypeException();
            }

            return new Regex(valueNode.GetValue(), RegexOptions.Compiled);
        }

        public DataNode TypeToNode(Regex value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }
    }
}
