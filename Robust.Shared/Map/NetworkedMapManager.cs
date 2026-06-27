using System;
using Robust.Shared.Timing;

namespace Robust.Shared.Map;

[Obsolete]
internal interface INetworkedMapManager : IMapManagerInternal
{
    [Obsolete]
    void CullDeletionHistory(GameTick upToTick);
}

[Obsolete]
internal sealed class NetworkedMapManager : MapManager, INetworkedMapManager
{
    [Obsolete]
    public void CullDeletionHistory(GameTick upToTick)
    {
        MapSystem.CullDeletionHistory(upToTick);
    }
}
