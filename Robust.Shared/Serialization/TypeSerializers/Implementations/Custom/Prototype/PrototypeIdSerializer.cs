using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype
{
    public sealed class AbstractPrototypeIdSerializer<TPrototype> : PrototypeIdSerializer<TPrototype>
        where TPrototype : class, IPrototype, IInheritingPrototype
    {
        public override ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return dependencies.Resolve<IPrototypeManager>().HasMapping<TPrototype>(node.Value)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"PrototypeID {node.Value} for type {typeof(TPrototype)} not found");
        }
    }

    /// <summary>
    /// Checks that a string corresponds to a valid prototype id. Note that any data fields using this serializer will
    /// also be validated by <see cref="IPrototypeManager.ValidateFields"/>
    /// </summary>
    [Virtual]
    public class PrototypeIdSerializer<TPrototype> : ITypeValidator<string, ValueDataNode> where TPrototype : class, IPrototype
    {
        public virtual ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return dependencies.Resolve<IPrototypeManager>().HasIndex<TPrototype>(node.Value)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"PrototypeID {node.Value} for type {typeof(TPrototype)} at {node.Start} not found");
        }
    }
}
