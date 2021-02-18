using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    public class FormattedMessageSerializer : ITypeSerializer<FormattedMessage>
    {
        public FormattedMessage NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueNode)
            {
                throw new InvalidNodeTypeException();
            }

            return FormattedMessage.FromMarkup(valueNode.GetValue());
        }

        public IDataNode TypeToNode(FormattedMessage value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }
    }
}
