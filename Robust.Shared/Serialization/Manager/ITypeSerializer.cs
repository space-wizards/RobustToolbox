using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<TType, TNode> :
        ITypeReader<TType, TNode>,
        ITypeWriter<TType>,
        ITypeCopier<TType>
        where TType : notnull where TNode : DataNode
    {
    }
}
