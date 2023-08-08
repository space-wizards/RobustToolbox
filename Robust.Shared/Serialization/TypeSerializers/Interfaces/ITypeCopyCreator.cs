using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public interface ITypeCopyCreator<TType> : BaseSerializerInterfaces.ITypeInterface<TType>
{
    [MustUseReturnValue]
    TType CreateCopy(
        ISerializationManager serializationManager,
        TType source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
