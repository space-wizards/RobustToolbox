using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class FormattedMessageSerializer : ITypeSerializer<FormattedMessage, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager,
            ValueDataNode node, bool skipHook, ISerializationContext? context = null)
        {
            return new DeserializedValue<FormattedMessage>(FormattedMessage.FromMarkup(node.Value));
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return FormattedMessage.ValidMarkup(node.Value) ? new ValidatedValueNode(node) : new ErrorNode(node, "Invalid markup in FormattedMessage.");
        }

        public DataNode Write(ISerializationManager serializationManager, FormattedMessage value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public FormattedMessage Copy(ISerializationManager serializationManager, FormattedMessage source,
            FormattedMessage target, bool skipHook, ISerializationContext? context = null)
        {
            return new(source);
        }
    }
}
