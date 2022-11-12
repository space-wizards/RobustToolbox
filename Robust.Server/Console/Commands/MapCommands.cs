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
    sealed class AddMapCommand : LocalizedCommands
    {
        public override string Command => "addmap";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    sealed class RemoveMapCommand : LocalizedCommands
    {
        public override string Command => "rmmap";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    public sealed class SaveGridCommand : LocalizedCommands
    {
        public override string Command => "savegrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

            IoCManager.Resolve<IMapLoader>().SaveGrid(gridId, args[1]);
            shell.WriteLine("Save successful. Look in the user data directory.");
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savebp-id"));
                case 2:
                    var res = IoCManager.Resolve<IResourceManager>();
                    var opts = CompletionHelper.UserFilePath(args[1], res.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
            }
            return CompletionResult.Empty;
        }
    }

    public sealed class LoadGridCommand : LocalizedCommands
    {
        public override string Command => "loadgrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
                if (!float.TryParse(args[2], out var x))
                {
                    shell.WriteError($"{args[2]} is not a valid float.");
                    return;
                }

                if (!float.TryParse(args[3], out var y))
                {
                    shell.WriteError($"{args[3]} is not a valid float.");
                    return;
                }

                loadOptions.Offset = new Vector2(x, y);
            }

            if (args.Length >= 5)
            {
                if (!float.TryParse(args[4], out var rotation))
                {
                    shell.WriteError($"{args[4]} is not a valid float.");
                    return;
                }

                loadOptions.Rotation = Angle.FromDegrees(rotation);
            }

            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError($"{args[5]} is not a valid boolean.");
                    return;
                }

                loadOptions.StoreMapUids = storeUids;
            }

            var mapLoader = IoCManager.Resolve<IMapLoader>();
            mapLoader.LoadGrid(mapId, args[1], loadOptions);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return LoadMap.GetCompletionResult(shell, args);
        }
    }

    public sealed class SaveMap : LocalizedCommands
    {
        public override string Command => "savemap";

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    public sealed class LoadMap : LocalizedCommands
    {
        public override string Command => "loadmap";

        public static CompletionResult GetCompletionResult(IConsoleShell shell, string[] args)
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

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return GetCompletionResult(shell, args);
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
                shell.WriteLine(Loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-loadmap-error", ("path", args[1])));
        }
    }

    sealed class LocationCommand : LocalizedCommands
    {
        public override string Command => "loc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    sealed class TpGridCommand : LocalizedCommands
    {
        public override string Command => "tpgrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteError("Wrong number of args.");
            }

            var gridId = EntityUid.Parse(args[0]);
            var xpos = float.Parse(args[1], CultureInfo.InvariantCulture);
            var ypos = float.Parse(args[2], CultureInfo.InvariantCulture);

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (mapManager.TryGetGrid(gridId, out var grid))
            {
                var gridXform = entManager.GetComponent<TransformComponent>(grid.Owner);
                var mapId = args.Length == 4 ? new MapId(int.Parse(args[3])) : gridXform.MapID;

                gridXform.Coordinates =
                    new EntityCoordinates(mapManager.GetMapEntityId(mapId), new Vector2(xpos, ypos));

                shell.WriteLine("Grid was teleported.");
            }
        }
    }

    sealed class RemoveGridCommand : LocalizedCommands
    {
        public override string Command => "rmgrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    internal sealed class RunMapInitCommand : LocalizedCommands
    {
        public override string Command => "mapinit";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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

    internal sealed class ListMapsCommand : LocalizedCommands
    {
        public override string Command => "lsmap";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var msg = new StringBuilder();

            foreach (var mapId in mapManager.GetAllMapIds().OrderBy(id => id.Value))
            {
                msg.AppendFormat("{0}: init: {1}, paused: {2}, ent: {3}, grids: {4}\n",
                    mapId, mapManager.IsMapInitialized(mapId),
                    mapManager.IsMapPaused(mapId),
                    mapManager.GetMapEntityId(mapId),
                    string.Join(",", mapManager.GetAllMapGrids(mapId).Select(grid => grid.Owner)));
            }

            shell.WriteLine(msg.ToString());
        }
    }

    internal sealed class ListGridsCommand : LocalizedCommands
    {
        public override string Command => "lsgrid";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var msg = new StringBuilder();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();

            foreach (var grid in mapManager.GetAllGrids().OrderBy(grid => grid.Owner))
            {
                var xform = xformQuery.GetComponent(grid.Owner);
                var worldPos = xform.WorldPosition;

                msg.AppendFormat("{0}: map: {1}, ent: {2}, pos: {3:0.0},{4:0.0} \n",
                    grid.Owner, xform.MapID, grid.Owner, worldPos.X, worldPos.Y);
            }

            shell.WriteLine(msg.ToString());
        }
    }
}
