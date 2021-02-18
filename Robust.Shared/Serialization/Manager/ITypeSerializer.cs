using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<TType, TNode> : ITypeReader<TType, TNode>, ITypeWriter<TType>
        where TType : notnull where TNode : DataNode
    {
    }

    public interface ITypeReader<TType, TNode> where TType : notnull where TNode : DataNode
    {
        TType Read(TNode node, ISerializationContext? context = null);
    }

    public interface ITypeWriter<TType> where TType : notnull
    {
        DataNode Write(TType value, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
