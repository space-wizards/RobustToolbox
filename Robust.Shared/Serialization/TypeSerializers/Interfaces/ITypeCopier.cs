using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeCopier<TType>
    {
        [MustUseReturnValue]
        TType Copy(
            ISerializationManager serializationManager,
            TType source,
            TType target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null);
    }
}
