using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Percents;

[TypeSerializer]
public sealed class PercentageRangeSerializer : ITypeSerializer<PercentageRange, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return PercentageSerializerUtility.TryParseRange(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing values for percentage range");
    }

    public PercentageRange Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<PercentageRange>? instanceProvider = null)
    {
        if (!PercentageSerializerUtility.TryParseRange(node.Value, out var range))
            throw new InvalidMappingException("Could not parse percentage range");

        return range.Value;
    }

    public DataNode Write(ISerializationManager serializationManager, PercentageRange value, IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToString());
    }
}
