using System.Diagnostics.Contracts;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects;

public sealed class MapSystem : SharedMapSystem
{
    [Pure]
    internal override MapId GetNextMapId()
    {
        // Client-side map entities use negative map Ids to avoid conflict with server-side maps.
        var id = new MapId(LastMapId - 1);
        while (MapExists(id) || UsedIds.Contains(id))
        {
            id = new MapId(id.Value - 1);
        }
        return id;
    }
}
