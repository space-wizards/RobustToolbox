using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects;

public sealed class MapSystem : SharedMapSystem
{
    protected override MapId GetNextMapId()
    {
        // Client-side map entities use negative map Ids to avoid conflict with server-side maps.
        var id = new MapId(--LastMapId);
        while (MapExists(id) || UsedIds.Contains(id))
        {
            id = new MapId(--LastMapId);
        }
        return id;
    }
}
