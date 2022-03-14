using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public interface ITypeInheritanceHandler<[UsedImplicitly] TType, TNode> where TNode : DataNode
{
    TNode PushInheritance(ISerializationManager serializationManager, TNode child, TNode parent,
        IDependencyCollection dependencies);
}
