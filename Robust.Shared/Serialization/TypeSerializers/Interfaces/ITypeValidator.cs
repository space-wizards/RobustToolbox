using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeValidator<[UsedImplicitly]TType, TNode> : BaseSerializerInterfaces.ITypeNodeInterface<TType, TNode> where TNode : DataNode
    {
        ValidationNode Validate(
            ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null);
    }
}
