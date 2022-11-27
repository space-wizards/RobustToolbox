using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public sealed class AbstractPrototypeIdListSerializer<T> : PrototypeIdListSerializer<T> where T : class, IPrototype, IInheritingPrototype
    {
        protected override PrototypeIdSerializer<T> PrototypeSerializer => new AbstractPrototypeIdSerializer<T>();
    }

    [Virtual]
    public partial class PrototypeIdListSerializer<T> : ITypeValidator<List<string>, SequenceDataNode> where T : class, IPrototype
    {
        protected virtual PrototypeIdSerializer<T> PrototypeSerializer => new();

        private ValidationNode ValidateInternal(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
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
