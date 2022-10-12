using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class MapIdSerializer : ITypeSerializer<MapId, ValueDataNode>
    {
        public MapId Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, MapId value = default)
        {
            var val = Parse.Int32(node.Value);
            return new MapId(val);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return Parse.TryInt32(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing MapId");
        }

        public DataNode Write(ISerializationManager serializationManager, MapId value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var val = (int)value;
            return new ValueDataNode(val.ToString());
        }

        [MustUseReturnValue]
        public MapId Copy(ISerializationManager serializationManager, MapId source, MapId target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.Value);
        }
    }
}
