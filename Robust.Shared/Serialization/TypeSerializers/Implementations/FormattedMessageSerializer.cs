using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class FormattedMessageSerializer : ITypeSerializer<FormattedMessage, ValueDataNode>
    {
        private LocStringSerializer _loc = new();

        public FormattedMessage Read(ISerializationManager serializationManager,
            ValueDataNode node, IDependencyCollection dependencies, bool skipHook,
            ISerializationContext? context = null, FormattedMessage? value = default)
        {
            return FormattedMessage.FromMarkup(_loc.Read(serializationManager, node, dependencies, skipHook, context));
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var valNode = _loc.Validate(serializationManager, node, dependencies, context);
            if (valNode is ErrorNode) return valNode;

            return FormattedMessage.ValidMarkup(node.Value)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Invalid markup in FormattedMessage.");
        }

        public DataNode Write(ISerializationManager serializationManager, FormattedMessage value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return _loc.Write(serializationManager, value.ToMarkup(), dependencies, alwaysWrite, context);
        }

        [MustUseReturnValue]
        public FormattedMessage Copy(ISerializationManager serializationManager, FormattedMessage source,
            FormattedMessage target, bool skipHook, ISerializationContext? context = null)
        {
            return new(source);
        }
    }
}
