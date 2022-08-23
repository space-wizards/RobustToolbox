using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeReaderWriter<TType, TNode> :
        ITypeReader<TType, TNode>,
        ITypeWriter<TType>
        where TNode : DataNode
    {
    }
}
