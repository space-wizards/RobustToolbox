using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    public class FormattedMessageSerializer : ITypeSerializer<FormattedMessage>
    {
        public FormattedMessage NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueNode)
            {
                throw new InvalidNodeTypeException();
            }

            return FormattedMessage.FromMarkup(valueNode.GetValue());
        }

        public DataNode TypeToNode(FormattedMessage value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }
    }
}
