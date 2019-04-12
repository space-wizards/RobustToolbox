using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Robust.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        IMapGrid LoadBlueprint(IMap map, string path);
        void SaveBlueprint(GridId gridId, string yamlPath);

        void LoadMap(MapId mapId, string path);
        void SaveMap(IMap map, string yamlPath);
    }
}
