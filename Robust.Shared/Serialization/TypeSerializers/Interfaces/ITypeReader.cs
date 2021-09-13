using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeReader<TType, TNode> : ITypeValidator<TType, TNode> where TNode : DataNode
    {
        DeserializationResult Read(
            ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null);
    }
}
