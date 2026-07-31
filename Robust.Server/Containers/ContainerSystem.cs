using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Robust.Server.Containers;

public sealed class ContainerSystem : SharedContainerSystem
{
    protected override void ValidateMissingEntity(EntityUid uid, BaseContainer cont, EntityUid missing)
    {
        Log.Error($"Missing entity for container {ToPrettyString(uid)}. Missing uid: {missing}");
        //cont.InternalRemove(ent);
    }
}
