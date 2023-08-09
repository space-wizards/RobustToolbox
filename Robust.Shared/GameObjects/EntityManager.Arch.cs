using Arch.Core;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    protected World World = default!;

    protected virtual void InitializeArch()
    {
        World = World.Create();
    }

    protected virtual void ShutdownArch()
    {
        World.Destroy(World);
    }

    protected virtual void DestroyArch(EntityUid uid)
    {
        World.Destroy(uid.ToArch());
    }

    protected virtual void SpawnEntityArch(EntityUid uid) {}
}
