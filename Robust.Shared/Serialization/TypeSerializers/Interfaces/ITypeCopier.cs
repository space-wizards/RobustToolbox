using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

public interface ITypeCopier<TType> : BaseSerializerInterfaces.ITypeInterface<TType>
{
    void CopyTo(
        ISerializationManager serializationManager,
        TType source,
        ref TType target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

}
