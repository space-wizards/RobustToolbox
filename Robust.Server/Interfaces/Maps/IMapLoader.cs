using System;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        IMapGrid? LoadBlueprint(MapId mapId, string path);
        void SaveBlueprint(GridId gridId, string yamlPath);

        void LoadMap(MapId mapId, string path);
        void SaveMap(MapId mapId, string yamlPath);

        event Action<YamlStream, string> LoadedMapData;
    }
}
