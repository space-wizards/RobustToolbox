using JetBrains.Annotations;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public interface ITypeInheritanceHandler<[UsedImplicitly]TType, TNode> where TNode : DataNode
{
    TNode PushInheritance(TNode child, TNode parent);
}
