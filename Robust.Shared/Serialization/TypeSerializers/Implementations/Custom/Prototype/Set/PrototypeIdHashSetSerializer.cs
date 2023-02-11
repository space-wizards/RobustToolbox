using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set
{
    public sealed class AbstractPrototypeIdHashSetSerializer<TPrototype> : PrototypeIdHashSetSerializer<TPrototype>
        where TPrototype : class, IPrototype, IInheritingPrototype
    {
        protected override PrototypeIdSerializer<TPrototype> PrototypeSerializer =>
            new AbstractPrototypeIdSerializer<TPrototype>();
    }

    [Virtual]
    public class PrototypeIdHashSetSerializer<TPrototype> :
        ITypeValidator<HashSet<string>, SequenceDataNode>,
        ITypeValidator<ImmutableHashSet<string>, SequenceDataNode>,
        ITypeValidator<ISet<string>, SequenceDataNode>,
        ITypeValidator<IReadOnlySet<string>, SequenceDataNode>
        where TPrototype : class, IPrototype
    {
        protected virtual PrototypeIdSerializer<TPrototype> PrototypeSerializer => new();

        public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            var list = new List<ValidationNode>();

            foreach (var dataNode in node.Sequence)
            {
                if (dataNode is not ValueDataNode value)
                {
                    list.Add(new ErrorNode(dataNode, $"Cannot cast node {dataNode} to ValueDataNode."));
                    continue;
                }

                list.Add(PrototypeSerializer.Validate(serializationManager, value, dependencies, context));
            }

            return new ValidatedSequenceNode(list);
        }
    }
}
