using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    public interface IMapLoader
    {
        (IReadOnlyList<EntityUid>, GridId?) LoadBlueprint(MapId mapId, string path);
        (IReadOnlyList<EntityUid>, GridId?) LoadBlueprint(MapId mapId, string path, MapLoadOptions options);
        void SaveBlueprint(GridId gridId, string yamlPath);

        (IReadOnlyList<EntityUid>, IReadOnlyList<GridId>) LoadMap(MapId mapId, string path);
        (IReadOnlyList<EntityUid>, IReadOnlyList<GridId>) LoadMap(MapId mapId, string path, MapLoadOptions options);
        void SaveMap(MapId mapId, string yamlPath);

        event Action<YamlStream, string> LoadedMapData;
    }
}
