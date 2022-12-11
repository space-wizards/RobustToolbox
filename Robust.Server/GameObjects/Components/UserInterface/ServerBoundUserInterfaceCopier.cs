using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Server.GameObjects;

[TypeSerializer]
public sealed class ServerBoundUserInterfaceCopier : ITypeCopyCreator<BoundUserInterface>
{
    public BoundUserInterface CreateCopy(ISerializationManager serializationManager, BoundUserInterface source, bool skipHook,
        ISerializationContext? context = null)
    {
        return new BoundUserInterface(source.RequireInputValidation, source.UiKey, source.InteractionRangeSqrd);
    }
}
