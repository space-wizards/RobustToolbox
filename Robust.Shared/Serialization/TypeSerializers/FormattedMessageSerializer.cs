using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class FormattedMessageSerializer : ITypeSerializer<FormattedMessage, ValueDataNode>
    {
        public DeserializationResult<FormattedMessage> Read(ValueDataNode node, ISerializationContext? context = null)
        {
            return new DeserializedValue<FormattedMessage>(FormattedMessage.FromMarkup(node.Value));
        }

        public DataNode Write(FormattedMessage value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public FormattedMessage Copy(FormattedMessage source, FormattedMessage target)
        {
            return new(source);
        }
    }
}
