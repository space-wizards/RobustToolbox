using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Robust.Server.Containers
{
    public sealed class ContainerSystem : SharedContainerSystem
    {
        protected override void ValidateMissingEntity(EntityUid uid, IContainer cont, EntityUid missing)
        {
            Logger.Error($"Missing entity for container {ToPrettyString(uid)}. Missing uid: {missing}");
            //cont.InternalRemove(ent);
        }
    }
}
