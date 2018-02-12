using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;

namespace SS14.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        void LoadBlueprint(IMap map, GridId newId, string path);
        void SaveBlueprint(IMap map, GridId gridId, string yamlPath);

        void LoadMap(MapId mapId, string path);
        void SaveMap(IMap map, string yamlPath);
    }
}
