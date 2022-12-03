using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public interface ITypeCopyCreator<TType> : BaseSerializerInterfaces.ITypeInterface<TType>
{
    [MustUseReturnValue]
    TType CreateCopy(
        ISerializationManager serializationManager,
        TType source,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
