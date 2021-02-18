using System.Text.RegularExpressions;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class RegexSerializer : ITypeSerializer<Regex>
    {
        public Regex NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueNode)
            {
                throw new InvalidNodeTypeException();
            }

            return new Regex(valueNode.GetValue(), RegexOptions.Compiled);
        }

        public IDataNode TypeToNode(Regex value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }
    }
}
