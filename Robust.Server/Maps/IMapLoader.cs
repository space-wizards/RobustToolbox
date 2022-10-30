using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Server.Maps
{
    public interface IMapLoader
    {
        (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadGrid(MapId mapId, string path);
        (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadGrid(MapId mapId, string path, MapLoadOptions options);

        (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path);
        (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path, MapLoadOptions options);
    }
}
