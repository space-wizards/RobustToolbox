using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Robust.Server.Interfaces.Maps
{
    public interface IMapLoader
    {
        IMapGrid LoadBlueprint(IMap map, string path, BlueprintLoadOptions options = null);
        void SaveBlueprint(GridId gridId, string yamlPath);

        void LoadMap(MapId mapId, string path);
        void SaveMap(IMap map, string yamlPath);
    }

    public sealed class BlueprintLoadOptions
    {
        public bool DoMapInit { get; set; } = true;
    }
}
