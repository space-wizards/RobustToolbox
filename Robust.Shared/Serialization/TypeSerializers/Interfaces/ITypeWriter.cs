using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeWriter<TType>
    {
        DataNode Write(ISerializationManager serializationManager, TType value, IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
