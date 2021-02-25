using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<TType, TNode> :
        ITypeReaderWriter<TType, TNode>,
        ITypeCopier<TType>
        where TType : notnull
        where TNode : DataNode
    {
    }
}
