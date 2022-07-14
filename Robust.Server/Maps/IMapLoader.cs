using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    public interface IMapLoader
    {
        (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadBlueprint(MapId mapId, string path);
        (IReadOnlyList<EntityUid> entities, EntityUid? gridId) LoadBlueprint(MapId mapId, string path, MapLoadOptions options);
        void SaveBlueprint(EntityUid gridId, string yamlPath);

        (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path);
        (IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> gridIds) LoadMap(MapId mapId, string path, MapLoadOptions options);
        void SaveMap(MapId mapId, string yamlPath);

        event Action<YamlStream, string> LoadedMapData;
    }
}
