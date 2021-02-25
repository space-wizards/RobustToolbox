using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeWriter<TType> where TType : notnull
    {
        DataNode Write(TType value, bool alwaysWrite = false,
            ISerializationContext? context = null);
    }
}
