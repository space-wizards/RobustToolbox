using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeCopier<TType>
    {
        [MustUseReturnValue]
        TType Copy(ISerializationManager serializationManager, TType source, TType target,
            ISerializationContext? context = null);
    }
}
