using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeWriter<TType> where TType : notnull
    {
        DataNode Write(ISerializationManager serializationManager, TType value, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
