using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Robust.Shared.Map;

internal interface INetworkedMapManager : IMapManagerInternal
{
    void CullDeletionHistory(GameTick upToTick);
}

internal sealed class NetworkedMapManager : MapManager, INetworkedMapManager
{
    public void CullDeletionHistory(GameTick upToTick)
    {
        var query = EntityManager.AllEntityQueryEnumerator<MapGridComponent>();

        while (query.MoveNext(out var grid))
        {
            var chunks = grid.ChunkDeletionHistory;
            chunks.RemoveAll(t => t.tick < upToTick);
        }
    }
}
