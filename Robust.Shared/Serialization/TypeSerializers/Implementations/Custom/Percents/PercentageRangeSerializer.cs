using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Percents;

public sealed class PercentageRangeSerializer : ITypeSerializer<(float,float), ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return PercentageSerializerUtility.TryParseRange(node.Value, out var range) && range.Length == 2
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing values for percentage range");
    }

    public (float, float) Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<(float, float)>? instanceProvider = null)
    {
        if (!PercentageSerializerUtility.TryParseRange(node.Value, out var range) || range.Length != 2)
            throw new InvalidMappingException("Could not parse percentage range");

        return (range[0], range[1]);
    }

    public DataNode Write(ISerializationManager serializationManager, (float, float) value, IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode($"{value.Item1 * 100}%, {value.Item2 * 100}%");
    }
}
