using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeValidator<[UsedImplicitly]TType, TNode>
    {
        ValidationNode Validate(
            ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null);
    }
}
