using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class ColorSerializer : ITypeSerializer<Color, ValueDataNode>, ITypeCopyCreator<Color>
    {
        public Color Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Color>? instanceProvider = null)
        {
            var deserializedColor = Color.TryFromName(node.Value, out var color)
                ? color :
                Color.FromHex(node.Value);

            return deserializedColor;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return Color.TryFromName(node.Value, out _) || Color.TryFromHex(node.Value) != null
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing Color.");
        }

        public DataNode Write(ISerializationManager serializationManager, Color value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToHex());
        }

        [MustUseReturnValue]
        public Color CreateCopy(ISerializationManager serializationManager, Color source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source.R, source.G, source.B, source.A);
        }
    }
}
