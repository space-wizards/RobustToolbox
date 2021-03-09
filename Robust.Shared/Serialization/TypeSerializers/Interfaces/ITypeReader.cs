using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeReader<[UsedImplicitly]TType, TNode> : ITypeValidator<TType, TNode> where TType : notnull where TNode : DataNode
    {
        DeserializationResult Read(ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null);
    }
}
