using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

public sealed class AbstractPrototypeIdArraySerializer<TPrototype> : PrototypeIdArraySerializer<TPrototype> where TPrototype : class, IPrototype, IInheritingPrototype
{
    protected override PrototypeIdSerializer<TPrototype> PrototypeSerializer =>
        new AbstractPrototypeIdSerializer<TPrototype>();
}

[Virtual]
public class PrototypeIdArraySerializer<TPrototype> : ITypeSerializer<string[], SequenceDataNode>,
    ITypeSerializer<string[], ValueDataNode> where TPrototype : class, IPrototype
{
    protected virtual PrototypeIdSerializer<TPrototype> PrototypeSerializer => new();

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedSequenceNode(node.Select(x =>
            x is ValueDataNode valueDataNode
                ? PrototypeSerializer.Validate(serializationManager, valueDataNode, dependencies, context)
                : new ErrorNode(x, $"Cannot cast node {x} to ValueDataNode.")).ToList());
    }

    public string[] Read(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, string[]? value = default)
    {
        return node.Select(x =>
                PrototypeSerializer.Read(serializationManager, (ValueDataNode)x, dependencies, skipHook, context))
            .ToArray();
    }

    public DataNode Write(ISerializationManager serializationManager, string[] value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value, alwaysWrite, context);
    }

    public string[] Copy(ISerializationManager serializationManager, string[] source, string[] target, bool skipHook,
        ISerializationContext? context = null)
    {
        serializationManager.Copy(source, ref target, context, skipHook);
        return target;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null) =>
        PrototypeSerializer.Validate(serializationManager, node, dependencies, context);

    public string[] Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, string[]? value = default) =>
        new[] { PrototypeSerializer.Read(serializationManager, node, dependencies, skipHook, context) };
}

