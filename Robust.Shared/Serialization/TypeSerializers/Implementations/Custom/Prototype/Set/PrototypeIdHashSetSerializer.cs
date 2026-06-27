using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set
{
    [Obsolete("Use ProtoId instead")]
    public sealed class PrototypeIdHashSetSerializer<TPrototype> :
        ITypeValidator<HashSet<string>, SequenceDataNode>,
        ITypeValidator<ImmutableHashSet<string>, SequenceDataNode>,
        ITypeValidator<ISet<string>, SequenceDataNode>,
        ITypeValidator<IReadOnlySet<string>, SequenceDataNode>
        where TPrototype : class, IPrototype
    {
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

                list.Add(ProtoIdSerializer<TPrototype>.Validate(dependencies, value));
            }

            return new ValidatedSequenceNode(list);
        }
    }
}
