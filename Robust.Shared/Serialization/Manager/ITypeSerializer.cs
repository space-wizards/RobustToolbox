using Robust.Shared.IoC;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<T>
    {
        T NodeToType(IDataNode node, ISerializationContext? context = null);
        IDataNode TypeToNode(T value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
