using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeReaderWriter<TType, TNode> :
        ITypeReader<TType, TNode>,
        ITypeWriter<TType>
        where TType : notnull
        where TNode : DataNode
    {
    }
}
