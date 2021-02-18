using Robust.Shared.IoC;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<TType, TNode> where TNode : DataNode
    {
        TType NodeToType(DataNode node, ISerializationContext? context = null);
        DataNode TypeToNode(TType value, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
