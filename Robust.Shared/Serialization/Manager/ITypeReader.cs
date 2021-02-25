using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeReader<TType, TNode> where TType : notnull where TNode : DataNode
    {
        TType Read(TNode node, ISerializationContext? context = null);
    }

}
