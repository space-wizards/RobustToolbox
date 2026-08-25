using JetBrains.Annotations;
using Robust.Shared.ColorNaming;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed partial class ColorSerializer : ITypeSerializer<Color, ValueDataNode>, ITypeCopyCreator<Color>
{
    [Dependency] private IPaletteManager _paletteMan = default!;

    public Color Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Color>? instanceProvider = null)
    {
        if (Color.TryFromName(node.Value, out var color))
            return color;

        // FIXME: breakpoint target, should be removed
        if (node.Value.Contains("."))
        {
            var i = 0;
            i++;
        }

        if (_paletteMan.TryGetQualifiedColor(node.Value, out var qualifiedColor))
            return qualifiedColor.Value;

        return Color.FromHex(node.Value);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return Color.TryFromName(node.Value, out _) || _paletteMan.TryGetQualifiedColor(node.Value, out _) || Color.TryFromHex(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing Color.");
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        Color value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToHex());
    }

    [MustUseReturnValue]
    public Color CreateCopy(
        ISerializationManager serializationManager,
        Color source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return new Color(source.R, source.G, source.B, source.A);
    }
}
