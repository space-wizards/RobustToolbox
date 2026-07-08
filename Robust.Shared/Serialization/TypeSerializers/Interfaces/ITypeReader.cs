using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeReader<TType, TNode> : ITypeValidator<TType, TNode> where TNode : DataNode
    {
        /// <summary>
        /// Method to read <see cref="TType"/> from <see cref="TNode"/>. When throwing errors try to include the line number from node.Start
        /// </summary>
        TType Read(ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<TType>? instanceProvider = null);
    }
}
