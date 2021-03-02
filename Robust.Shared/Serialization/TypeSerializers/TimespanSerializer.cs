using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class TimespanSerializer : ITypeSerializer<TimeSpan, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
            return new DeserializedValue<TimeSpan>(TimeSpan.FromSeconds(seconds));
        }

        public ValidatedNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return double.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, TimeSpan value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        [MustUseReturnValue]
        public TimeSpan Copy(ISerializationManager serializationManager, TimeSpan source, TimeSpan target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
