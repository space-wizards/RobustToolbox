using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Server.Console.Commands
{
    class AddMapCommand : IConsoleCommand
    {
        public string Command => "addmap";
        public string Description => "Adds a new empty map to the round. If the mapID already exists, this command does nothing.";
        public string Help => "addmap <mapID> [initialize]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
                return;

            var mapId = new MapId(int.Parse(args[0]));

            var mapMgr = IoCManager.Resolve<IMapManager>();
            var pauseMgr = IoCManager.Resolve<IPauseManager>();

            if (!mapMgr.MapExists(mapId))
            {
                mapMgr.CreateMap(mapId);
                if (args.Length >= 2 && args[1] == "false")
                {
                    pauseMgr.AddUninitializedMap(mapId);
                }
                shell.WriteLine($"Map with ID {mapId} created.");
                return;
            }

            shell.WriteLine($"Map with ID {mapId} already exists!");
        }
    }

    class RemoveMapCommand : IConsoleCommand
    {
        public string Command => "rmmap";
        public string Description => "Removes a map from the world. You cannot remove nullspace.";
        public string Help => "rmmap <mapId>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var mapId = new MapId(int.Parse(args[0]));
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine($"Map {mapId.Value} does not exist.");
                return;
            }

            mapManager.DeleteMap(mapId);
            shell.WriteLine($"Map {mapId.Value} was removed.");
        }
    }

    public class SaveBp : IConsoleCommand
    {
        public string Command => "savebp";
        public string Description => "Serializes a grid to disk.";
        public string Help => "savebp <gridID> <Path>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine("Not enough arguments.");
                return;
            }

            if (!int.TryParse(args[0], out var intGridId))
            {
                shell.WriteLine("Not a valid grid ID.");
                return;
            }

            var gridId = new GridId(intGridId);

            var mapManager = IoCManager.Resolve<IMapManager>();

            // no saving default grid
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteLine("That grid does not exist.");
                return;
            }

            IoCManager.Resolve<IMapLoader>().SaveBlueprint(gridId, args[1]);
            shell.WriteLine("Save successful. Look in the user data directory.");
        }
    }

    public class LoadBp : IConsoleCommand
    {
        public string Command => "loadbp";
        public string Description => "Loads a blueprint from disk into the game.";
        public string Help => "loadbp <MapID> <Path> [storeUids]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            if (!int.TryParse(args[0], out var intMapId))
            {
                return;
            }

            var mapId = new MapId(intMapId);

            // no loading into null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteLine("Cannot load into nullspace.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("Target map does not exist.");
                return;
            }

            var loadOptions = new MapLoadOptions();
            if (args.Length > 2)
            {
                loadOptions.StoreMapUids = bool.Parse(args[2]);
            }

            var mapLoader = IoCManager.Resolve<IMapLoader>();
            mapLoader.LoadBlueprint(mapId, args[1], loadOptions);
        }
    }

    public class SaveMap : IConsoleCommand
    {
        public string Command => "savemap";
        public string Description => "Serializes a map to disk.";
        public string Help => "savemap <MapID> <Path>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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
            if (!mapManager.MapExists(mapID))
                return;

            // TODO: Parse path
            IoCManager.Resolve<IMapLoader>().SaveMap(mapID, "Maps/Demo/DemoMap.yaml");
        }
    }

    public class LoadMap : IConsoleCommand
    {
        public string Command => "loadmap";
        public string Description => "Loads a map from disk into the game.";
        public string Help => "loadmap <MapID> <Path>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
                return;

            if (!int.TryParse(args[0], out var intMapId))
                return;

            var mapID = new MapId(intMapId);

            // no loading null space
            if (mapID == MapId.Nullspace)
            {
                shell.WriteLine("You cannot load into map 0.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (mapManager.MapExists(mapID))
            {
                shell.WriteLine($"Map {mapID} already exists.");
                return;
            }

            // TODO: Parse path
            var mapPath = "Maps/Demo/DemoMap.yaml";
            IoCManager.Resolve<IMapLoader>().LoadMap(mapID, mapPath);
            shell.WriteLine($"Map {mapID} has been loaded from {mapPath}.");
        }
    }

    class LocationCommand : IConsoleCommand
    {
        public string Command => "loc";
        public string Description => "Prints the absolute location of the player's entity to console.";
        public string Help => "loc";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
                return;

            var pos = player.AttachedEntity.Transform.Coordinates;
            var entityManager = IoCManager.Resolve<IEntityManager>();

            shell.WriteLine($"MapID:{pos.GetMapId(entityManager)} GridID:{pos.GetGridId(entityManager)} X:{pos.X:N2} Y:{pos.Y:N2}");
        }
    }

    class PauseMapCommand : IConsoleCommand
    {
        public string Command => "pausemap";
        public string Description => "Pauses a map, pausing all simulation processing on it.";
        public string Help => "Usage: pausemap <map ID>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Need to supply a valid MapId");
                return;
            }

            var arg = args[0];
            var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

            var pauseManager = IoCManager.Resolve<IPauseManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("That map does not exist.");
                return;
            }
            pauseManager.SetMapPaused(mapId, true);
        }
    }

    class UnpauseMapCommand : IConsoleCommand
    {
        public string Command => "unpausemap";
        public string Description => "unpauses a map, resuming all simulation processing on it.";
        public string Help => "Usage: unpausemap <map ID>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Need to supply a valid MapId");
                return;
            }

            var arg = args[0];
            var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

            var pauseManager = IoCManager.Resolve<IPauseManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("That map does not exist.");
                return;
            }
            pauseManager.SetMapPaused(mapId, false);
        }
    }

    class QueryMapPausedCommand : IConsoleCommand
    {
        public string Command => "querymappaused";
        public string Description => "Check whether a map is paused or not.";
        public string Help => "Usage: querymappaused <map ID>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var arg = args[0];
            var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

            var pauseManager = IoCManager.Resolve<IPauseManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("That map does not exist.");
                return;
            }
            shell.WriteLine(pauseManager.IsMapPaused(mapId).ToString());
        }
    }

    class TpGridCommand : IConsoleCommand
    {
        public string Command => "tpgrid";
        public string Description => "Teleports a grid to a new location.";
        public string Help => "tpgrid <gridId> <X> <Y> [<MapId>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteLine("Wrong number of args.");
            }

            var gridId = new GridId(int.Parse(args[0]));
            var xpos = float.Parse(args[1]);
            var ypos = float.Parse(args[2]);

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (mapManager.TryGetGrid(gridId, out var grid))
            {
                var mapId = args.Length == 4 ? new MapId(int.Parse(args[3])) : grid.ParentMapId;

                grid.ParentMapId = mapId;
                grid.WorldPosition = new Vector2(xpos, ypos);

                shell.WriteLine("Grid was teleported.");
            }
        }
    }

    class RemoveGridCommand : IConsoleCommand
    {
        public string Command => "rmgrid";
        public string Description => "Removes a grid from a map. You cannot remove the default grid.";
        public string Help => "rmgrid <gridId>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var gridId = new GridId(int.Parse(args[0]));
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.GridExists(gridId))
            {
                shell.WriteLine($"Grid {gridId.Value} does not exist.");
                return;
            }

            mapManager.DeleteGrid(gridId);
            shell.WriteLine($"Grid {gridId.Value} was removed.");
        }
    }

    internal sealed class RunMapInitCommand : IConsoleCommand
    {
        public string Command => "mapinit";
        public string Description => "Runs map init on a map";
        public string Help => "mapinit <mapID>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var pauseManager = IoCManager.Resolve<IPauseManager>();

            var arg = args[0];
            var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("Map does not exist!");
                return;
            }

            if (pauseManager.IsMapInitialized(mapId))
            {
                shell.WriteLine("Map is already initialized!");
                return;
            }

            pauseManager.DoMapInitialize(mapId);
        }
    }

    internal sealed class ListMapsCommand : IConsoleCommand
    {
        public string Command => "lsmap";
        public string Description => "Lists maps";
        public string Help => "lsmap";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var pauseManager = IoCManager.Resolve<IPauseManager>();

            var msg = new StringBuilder();

            foreach (var mapId in mapManager.GetAllMapIds().OrderBy(id => id.Value))
            {
                msg.AppendFormat("{0}: init: {1}, paused: {2}, ent: {3}, grids: {4}\n",
                    mapId, pauseManager.IsMapInitialized(mapId),
                    pauseManager.IsMapPaused(mapId),
                    string.Join(",", mapManager.GetAllMapGrids(mapId).Select(grid => grid.Index)),
                    mapManager.GetMapEntityId(mapId));
            }

            shell.WriteLine(msg.ToString());
        }
    }

    internal sealed class ListGridsCommand : IConsoleCommand
    {
        public string Command => "lsgrid";
        public string Description => "List grids";
        public string Help => "lsgrid";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var msg = new StringBuilder();

            foreach (var grid in mapManager.GetAllGrids().OrderBy(grid => grid.Index.Value))
            {
                msg.AppendFormat("{0}: map: {1}, ent: {2}, pos: {3} \n",
                    grid.Index, grid.ParentMapId, grid.WorldPosition, grid.GridEntityId);
            }

            shell.WriteLine(msg.ToString());
        }
    }
}
