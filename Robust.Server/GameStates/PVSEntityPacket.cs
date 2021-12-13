using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameStates;

public struct PVSEntityPacket
{
    public readonly TransformComponent TransformComponent;
    public readonly MetaDataComponent MetaDataComponent;
    public readonly ContainerManagerComponent? ContainerManagerComponent;

    public PVSEntityPacket(IEntityManager entityManager, EntityUid uid)
    {
        TransformComponent = entityManager.GetComponent<TransformComponent>(uid);
        MetaDataComponent = entityManager.GetComponent<MetaDataComponent>(uid);
        entityManager.TryGetComponent(uid, out ContainerManagerComponent);
    }
}
