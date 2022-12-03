using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
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
public class PrototypeIdArraySerializer<TPrototype> : ITypeValidator<string[], SequenceDataNode>,
    ITypeValidator<string[], ValueDataNode> where TPrototype : class, IPrototype
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

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null) =>
        PrototypeSerializer.Validate(serializationManager, node, dependencies, context);
}

