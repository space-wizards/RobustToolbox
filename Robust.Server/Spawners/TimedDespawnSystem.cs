using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;

namespace Robust.Server.Spawners;

public sealed partial class TimedDespawnSystem : SharedTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}
