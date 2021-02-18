using Robust.Shared.IoC;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeSerializer<T>
    {
        T NodeToType(DataNode node, ISerializationContext? context = null);
        DataNode TypeToNode(T value, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
