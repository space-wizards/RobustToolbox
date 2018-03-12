using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Server.ServerConsole.Commands
{
    public class SaveBp : IConsoleCommand
    {
        public string Command => "savebp";
        public string Description => "Serializes a grid to disk.";
        public string Help => "savebp <mapID> <gridID> <Path>";

        public void Execute(params string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intMapId))
                return;

            if (!int.TryParse(args[1], out var intGridId))
                return;

            var mapID = new MapId(intMapId);
            var gridId = new GridId(intGridId);

            // no saving null space
            if(mapID == MapId.Nullspace)
                return;

            // no saving default grid
            if (gridId == GridId.DefaultGrid)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetMap(mapID, out var map))
                return;

            if (!map.GridExists(gridId))
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().SaveBlueprint(map, gridId, "Maps/Demo/DemoGrid.yaml");
        }
    }

    public class LoadBp : IConsoleCommand
    {
        public string Command => "loadbp";
        public string Description => "Loads a blueprint from disk into the game.";
        public string Help => "loadbp <MapID> <GridID> <Path>";

        public void Execute(params string[] args)
        {
            //TODO: Make me work after placement can create new grids.
        }
    }

    public class SaveMap : IConsoleCommand
    {
        public string Command => "savemap";
        public string Description => "Serializes a map to disk.";
        public string Help => "savemap <MapID> <Path>";

        public void Execute(params string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intMapId))
                return;

            var mapID = new MapId(intMapId);

            // no saving null space
            if (mapID == MapId.Nullspace)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetMap(mapID, out var map))
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().SaveMap(map, "Maps/Demo/DemoMap.yaml");
        }
    }

    public class LoadMap : IConsoleCommand
    {
        public string Command => "loadmap";
        public string Description => "Loads a map from disk into the game.";
        public string Help => "loadmap <MapID> <Path>";

        public void Execute(params string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intMapId))
                return;

            var mapID = new MapId(intMapId);

            // no loading null space
            if (mapID == MapId.Nullspace)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            if(mapManager.MapExists(mapID))
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().LoadMap(mapID, "Maps/Demo/DemoMap.yaml");
        }
    }
}
