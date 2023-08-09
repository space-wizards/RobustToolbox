using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

public sealed partial class ServerEntityManager
{
    protected override void SpawnEntityArch(EntityUid uid)
    {
        World.Create(uid.GetArchId());
    }
}
