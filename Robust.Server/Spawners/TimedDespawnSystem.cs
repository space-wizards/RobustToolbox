using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;

namespace Robust.Server.Spawners;

public sealed class TimedDespawnSystem : SharedTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}
