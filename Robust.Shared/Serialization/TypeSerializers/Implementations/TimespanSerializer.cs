using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class TimespanSerializer : ITypeSerializer<TimeSpan, ValueDataNode>, ITypeCopyCreator<TimeSpan>
    {
        public TimeSpan Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<TimeSpan>? instanceProvider = null)
        {
            var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds(seconds);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return double.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing TimeSpan");
        }

        public DataNode Write(ISerializationManager serializationManager, TimeSpan value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        [MustUseReturnValue]
        public TimeSpan CreateCopy(ISerializationManager serializationManager, TimeSpan source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return source;
        }
    }
}
