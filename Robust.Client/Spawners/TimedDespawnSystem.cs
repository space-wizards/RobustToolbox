using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;

namespace Robust.Client.Spawners;

public sealed partial class TimedDespawnSystem : SharedTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return IsClientSide(uid);
    }
}
