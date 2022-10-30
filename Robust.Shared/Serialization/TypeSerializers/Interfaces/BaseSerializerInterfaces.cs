using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public static class BaseSerializerInterfaces
{
    public interface ITypeInterface<TType>
    {

    }

    public interface ITypeNodeInterface<TType, TNode> where TNode : DataNode
    {

    }
}
