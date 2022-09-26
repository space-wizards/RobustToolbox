using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Server.Console.Commands
{
    sealed class AddMapCommand : IConsoleCommand
    {
        public string Command => "addmap";

        public string Description =>
            "Adds a new empty map to the round. If the mapID already exists, this command does nothing.";

        public string Help => "addmap <mapID> [initialize]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
                return;

            var mapId = new MapId(int.Parse(args[0]));

            var mapMgr = IoCManager.Resolve<IMapManager>();

            if (!mapMgr.MapExists(mapId))
            {
                mapMgr.CreateMap(mapId);
                if (args.Length >= 2 && args[1] == "false")
                {
                    mapMgr.AddUninitializedMap(mapId);
                }

                shell.WriteLine($"Map with ID {mapId} created.");
                return;
            }

            shell.WriteError($"Map with ID {mapId} already exists!");
        }
    }

    sealed class RemoveMapCommand : IConsoleCommand
    {
        public string Command => "rmmap";
        public string Description => "Removes a map from the world. You cannot remove nullspace.";
        public string Help => "rmmap <mapId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError("Wrong number of args.");
                return;
            }

            var mapId = new MapId(int.Parse(args[0]));
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError($"Map {mapId.Value} does not exist.");
                return;
            }

            mapManager.DeleteMap(mapId);
            shell.WriteLine($"Map {mapId.Value} was removed.");
        }
    }

    public sealed class SaveBp : IConsoleCommand
    {
        public string Command => "savebp";
        public string Description => "Serializes a grid to disk.";
        public string Help => "savebp <gridID> <Path>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteError("Not enough arguments.");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var gridId))
            {
                shell.WriteError("Not a valid entity ID.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();

            // no saving default grid
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteError("That grid does not exist.");
                return;
            }

            IoCManager.Resolve<IMapLoader>().SaveBlueprint(gridId, args[1]);
            shell.WriteLine("Save successful. Look in the user data directory.");
        }
    }

    public sealed class LoadBp : IConsoleCommand
    {
        public string Command => "loadbp";
        public string Description => "Loads a blueprint from disk into the game.";
        public string Help => "loadbp <MapID> <Path> [x y] [rotation] [storeUids]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2 || args.Length == 3 || args.Length > 6)
            {
                shell.WriteError("Must have either 2, 4, 5, or 6 arguments.");
                return;
            }

            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError($"{args[0]} is not a valid integer.");
                return;
            }

            var mapId = new MapId(intMapId);

            // no loading into null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError("Cannot load into nullspace.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError("Target map does not exist.");
                return;
            }

            var loadOptions = new MapLoadOptions();
            if (args.Length >= 4)
            {
                if (!int.TryParse(args[2], out var x))
                {
                    shell.WriteError($"{args[2]} is not a valid integer.");
                    return;
                }

                if (!int.TryParse(args[3], out var y))
                {
                    shell.WriteError($"{args[3]} is not a valid integer.");
                    return;
                }

                loadOptions.Offset = new Vector2(x, y);
            }

            if (args.Length >= 5)
            {
                if (!float.TryParse(args[4], out var rotation))
                {
                    shell.WriteError($"{args[4]} is not a valid integer.");
                    return;
                }

                loadOptions.Rotation = Angle.FromDegrees(rotation);
            }

            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError($"{args[5]} is not a valid boolean..");
                    return;
                }

                loadOptions.StoreMapUids = storeUids;
            }

            var mapLoader = IoCManager.Resolve<IMapLoader>();
            mapLoader.LoadBlueprint(mapId, args[1], loadOptions);
        }
    }

    public sealed class SaveMap : IConsoleCommand
    {
        public string Command => "savemap";
        public string Description => Loc.GetString("cmd-savemap-desc");
        public string Help => Loc.GetString("cmd-savemap-help");

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 2:
                    var res = IoCManager.Resolve<IResourceManager>();
                    var opts = CompletionHelper.UserFilePath(args[1], res.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 3:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-force"));
            }
            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteLine(Help);
                return;
            }

            var mapId = new MapId(intMapId);

            // no saving null space
            if (mapId == MapId.Nullspace)
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            if (mapManager.IsMapInitialized(mapId) &&
                ( args.Length < 3  || !bool.TryParse(args[2], out var force) || !force))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-init-warning"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", args[1])));
            IoCManager.Resolve<IMapLoader>().SaveMap(mapId, args[1]);
            shell.WriteLine(Loc.GetString("cmd-savemap-success"));
        }
    }

    public sealed class LoadMap : IConsoleCommand
    {
        public string Command => "loadmap";
        public string Description => Loc.GetString("cmd-loadmap-desc");
        public string Help => Loc.GetString("cmd-loadmap-help");

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 2:
                    var res = IoCManager.Resolve<IResourceManager>();
                    var opts = CompletionHelper.UserFilePath(args[1], res.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[1], res));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 3:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-x-position"));
                case 4:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-y-position"));
                case 5:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-rotation"));
                case 6:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-uids"));
            }

            return CompletionResult.Empty;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2 || args.Length > 6)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
                return;
            }

            var mapId = new MapId(intMapId);

            // no loading null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-loadmap-nullspace"));
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (mapManager.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadmap-exists", ("mapId", mapId)));
                return;
            }

            var loadOptions = new MapLoadOptions();

            float x = 0, y = 0;
            if (args.Length >= 3)
            {
                if (!float.TryParse(args[2], out x))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                    return;
                }
            }

            if (args.Length >= 4)
            {

                if (!float.TryParse(args[3], out y))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                    return;
                }
            }

            loadOptions.Offset = new Vector2(x, y);

            if (args.Length >= 5)
            {
                if (!float.TryParse(args[4], out var rotation))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                    return;
                }

                loadOptions.Rotation = new Angle(rotation);
            }

            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                    return;
                }

                loadOptions.StoreMapUids = storeUids;
            }

            IoCManager.Resolve<IMapLoader>().LoadMap(mapId, args[1], loadOptions);

            if (mapManager.MapExists(mapId))
                shell.WriteLine(Loc.GetString("cmd-loadmap-successt", ("mapId", mapId), ("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-loadmap-error", ("path", args[1])));
        }
    }

    sealed class LocationCommand : IConsoleCommand
    {
        public string Command => "loc";
        public string Description => "Prints the absolute location of the player's entity to console.";
        public string Help => "loc";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var pt = player?.AttachedEntityTransform;
            if (pt == null)
                return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var pos = pt.Coordinates;

            shell.WriteLine(
                $"MapID:{pos.GetMapId(entityManager)} GridUid:{pos.GetGridUid(entityManager)} X:{pos.X:N2} Y:{pos.Y:N2}");
        }
    }

    sealed class TpGridCommand : IConsoleCommand
    {
        public string Command => "tpgrid";
        public string Description => "Teleports a grid to a new location.";
        public string Help => "tpgrid <gridId> <X> <Y> [<MapId>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteError("Wrong number of args.");
            }

            var gridId = EntityUid.Parse(args[0]);
            var xpos = float.Parse(args[1], CultureInfo.InvariantCulture);
            var ypos = float.Parse(args[2], CultureInfo.InvariantCulture);

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

    sealed class RemoveGridCommand : IConsoleCommand
    {
        public string Command => "rmgrid";
        public string Description => "Removes a grid from a map. You cannot remove the default grid.";
        public string Help => "rmgrid <gridId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError("Wrong number of args.");
                return;
            }

            var gridId = EntityUid.Parse(args[0]);
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.GridExists(gridId))
            {
                shell.WriteError($"Grid {gridId} does not exist.");
                return;
            }

            mapManager.DeleteGrid(gridId);
            shell.WriteLine($"Grid {gridId} was removed.");
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
                shell.WriteError("Wrong number of args.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();

            var arg = args[0];
            var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

            if (!mapManager.MapExists(mapId))
            {
                shell.WriteError("Map does not exist!");
                return;
            }

            if (mapManager.IsMapInitialized(mapId))
            {
                shell.WriteError("Map is already initialized!");
                return;
            }

            mapManager.DoMapInitialize(mapId);
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

            var msg = new StringBuilder();

            foreach (var mapId in mapManager.GetAllMapIds().OrderBy(id => id.Value))
            {
                msg.AppendFormat("{0}: init: {1}, paused: {2}, ent: {3}, grids: {4}\n",
                    mapId, mapManager.IsMapInitialized(mapId),
                    mapManager.IsMapPaused(mapId),
                    mapManager.GetMapEntityId(mapId),
                    string.Join(",", mapManager.GetAllMapGrids(mapId).Select(grid => grid.Index)));
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
            var entManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var msg = new StringBuilder();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in mapManager.GetAllGrids().OrderBy(grid => grid.Index.Value))
            {
                var xform = xformQuery.GetComponent(grid.GridEntityId);
                var worldPos = xform.WorldPosition;

                msg.AppendFormat("{0}: map: {1}, ent: {2}, pos: {3:0.0},{4:0.0} \n",
                    grid.Index, xform.MapID, grid.GridEntityId, worldPos.X, worldPos.Y);
            }

            shell.WriteLine(msg.ToString());
        }
    }
}
