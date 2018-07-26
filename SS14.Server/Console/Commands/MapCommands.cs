using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Server.Console.Commands
{
    class AddMapCommand : IClientCommand
    {
        public string Command => "addmap";
        public string Description => "Adds a new empty map to the round. If the mapID already exists, this command does nothing.";
        public string Help => "addmap <mapID>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
                return;

            var mapId = new MapId(int.Parse(args[0]));

            var mapMgr = IoCManager.Resolve<IMapManager>();

            if (!mapMgr.MapExists(mapId))
            {
                mapMgr.CreateMap(mapId);
                shell.SendText(player, $"Map with ID {mapId} created.");
                return;
            }

            shell.SendText(player, $"Map with ID {mapId} already exists!");
        }
    }

    public class SaveBp : IClientCommand
    {
        public string Command => "savebp";
        public string Description => "Serializes a grid to disk.";
        public string Help => "savebp <gridID> <Path>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intGridId))
                return;

            var gridId = new GridId(intGridId);

            var mapManager = IoCManager.Resolve<IMapManager>();

            // no saving default grid
            if (!mapManager.TryGetGrid(gridId, out var grid) || grid.IsDefaultGrid)
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().SaveBlueprint(gridId, "Maps/Demo/DemoGrid.yaml");
        }
    }

    public class LoadBp : IClientCommand
    {
        public string Command => "loadbp";
        public string Description => "Loads a blueprint from disk into the game.";
        public string Help => "loadbp <MapID> <GridID> <Path>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            //TODO: Make me work after placement can create new grids.
        }
    }

    public class SaveMap : IClientCommand
    {
        public string Command => "savemap";
        public string Description => "Serializes a map to disk.";
        public string Help => "savemap <MapID> <Path>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
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

    public class LoadMap : IClientCommand
    {
        public string Command => "loadmap";
        public string Description => "Loads a map from disk into the game.";
        public string Help => "loadmap <MapID> <Path>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
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
            if (mapManager.MapExists(mapID))
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().LoadMap(mapID, "Maps/Demo/DemoMap.yaml");
        }
    }

    class LocationCommand : IClientCommand
    {
        public string Command => "loc";
        public string Description => "Prints the absolute location of the player's entity to console.";
        public string Help => "loc";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if(player.AttachedEntity == null)
                return;

            var pos = player.AttachedEntity.GetComponent<ITransformComponent>().LocalPosition;

            shell.SendText(player, $"MapID:{pos.MapID} GridID:{pos.GridID} X:{pos.X:N2} Y:{pos.Y:N2}");
        }
    }
}
