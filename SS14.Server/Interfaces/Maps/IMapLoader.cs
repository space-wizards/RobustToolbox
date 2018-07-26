using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;

namespace SS14.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        IMapGrid LoadBlueprint(IMap map, string path);
        void SaveBlueprint(GridId gridId, string yamlPath);

        void LoadMap(MapId mapId, string path);
        void SaveMap(IMap map, string yamlPath);
    }
}
