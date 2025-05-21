using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Server.Console.Commands
{
    public sealed class SaveGridCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _ent = default!;
        [Dependency] private readonly IResourceManager _resource = default!;
        [Dependency] private readonly IEntitySystemManager _system = default!;

        public override string Command => "savegrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 1 or > 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);
            EntityUid uid;

            switch (args.Length)
            {

                case 1:
                    var ent = shell.Player?.AttachedEntity;

                    // If there is no associated player or they don't have an attached entity, then we can't continue with just one parameter provided
                    if (ent is null)
                    {
                        shell.WriteError(Loc.GetString("cmd-savegrid-no-player-ent"));
                        return;
                    }

                    var gridEnt = _system.GetEntitySystem<TransformSystem>().GetGrid(ent.Value);

                    if (gridEnt is null)
                    {
                        shell.WriteLine(Help);
                        shell.WriteError(Loc.GetString("cmd-savegrid-no-player-grid"));
                        return;
                    }

                    uid = gridEnt.Value;
                    break;
                // Manually specified grid
                case 2:
                    if (!NetEntity.TryParse(args[1], out var gridNet))
                    {
                        shell.WriteError(Loc.GetString("cmd-savegrid-invalid-grid",("arg",args[1])));
                        return;
                    }

                    uid = _ent.GetEntity(gridNet);
                    break;
                default:
                    return;
            }

            // no saving default grid
            if (!_ent.EntityExists(uid))
            {
                shell.WriteError(Loc.GetString("cmd-savegrid-existnt",("uid",args[1])));
                return;
            }

            //TODO test if uid is actually a GRID

            bool saveSuccess = _ent.System<MapLoaderSystem>().TrySaveGrid(uid, path);
            if(saveSuccess)
            {
                shell.WriteLine(Loc.GetString("cmd-savegrid-success"));
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-savegrid-fail"));
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[1], _ent), Loc.GetString("cmd-hint-savebp-id"));
            }
            return CompletionResult.Empty;
        }
    }

    public sealed class LoadGridCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "loadgrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 1 or 3 or > 6)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);
            MapId mapId;
            Vector2 offset = default;
            Angle rot = default;
            var opts = DeserializationOptions.Default;

            if (args.Length == 1
                && shell.Player?.AttachedEntity is not null)
            {
                var ent = shell.Player.AttachedEntity;
                offset = _system.GetEntitySystem<TransformSystem>().GetWorldPosition(ent.Value);
                mapId = _system.GetEntitySystem<TransformSystem>().GetMapId(ent.Value);
                rot = _system.GetEntitySystem<TransformSystem>().GetWorldRotation(ent.Value);
                _system.GetEntitySystem<MapLoaderSystem>().TryLoadGrid(mapId, path, out _, opts, offset, rot);
                return;
            }

            //TODO make it autosuggest mapId-d

            //TODO make it autosuggest current coordinates?

            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadgrid-invalid-map-id",("arg",args[1])));
                return;
            }

            mapId = new MapId(intMapId);
            // no loading into null space
            if (mapId == MapId.Nullspace)
            {
                // shell.WriteError("Cannot load into nullspace.");
                shell.WriteError(Loc.GetString("cmd-loadgrid-nullspace-map"));
                return;
            }

            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (!sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadgrid-missing-map",("mapId",mapId)));
                return;
            }

            if (args.Length >= 4)
            {
                if (!float.TryParse(args[2], out var x))
                {
                    shell.WriteError(Loc.GetString("cmd-loadgrid-not-coordinate",("arg",args[2])));
                    return;
                }

                if (!float.TryParse(args[3], out var y))
                {
                    shell.WriteError(Loc.GetString("cmd-loadgrid-not-coordinate",("arg",args[3])));
                    return;
                }

                offset = new Vector2(x, y);
            }

            if (args.Length >= 5)
            {
                if (!float.TryParse(args[4], out var rotation))
                {
                    shell.WriteError(Loc.GetString("cmd-loadgrid-not-rotation",("arg",args[4])));
                    return;
                }

                rot = Angle.FromDegrees(rotation);
            }

            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError(Loc.GetString("cmd-loadgrid-not-boolean",("arg",args[5])));
                    return;
                }

                opts.StoreYamlUids = storeUids;
            }

            _system.GetEntitySystem<MapLoaderSystem>().TryLoadGrid(mapId, path, out _, opts, offset, rot);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return LoadMap.GetCompletionResult(shell, args, _resource);
        }
    }

    public sealed class SaveMap : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "savemap";

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entManager), Loc.GetString("cmd-hint-savemap-id"));
                case 3:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-force"));
            }
            return CompletionResult.Empty;
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 1 or > 3 )
            {
                shell.WriteLine(Help);
                return;
            }

            MapId mapId;

            switch (args.Length)
            {
                case 1:
                    var ent = shell.Player?.AttachedEntity;

                    // If there is no associated player or they don't have an attached entity, then we can't continue with just one parameter provided
                    if (ent is null)
                    {
                        shell.WriteError(Loc.GetString("cmd-savemap-no-player-ent"));
                        return;
                    }

                    mapId = _system.GetEntitySystem<TransformSystem>().GetMapId(ent.Value);
                    break;
                // Manually specified grid
                case 2:
                case 3:
                    if (!int.TryParse(args[1], out var intMapId))
                    {
                        shell.WriteError(Loc.GetString("cmd-savemap-invalid-map-id", ("arg", args[1])));
                        return;
                    }

                    mapId = new MapId(intMapId);
                    break;
                default:
                    return;
            }

            // no saving null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-savemap-nullspace"));
                return;
            }

            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (!sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            if (sys.IsInitialized(mapId) &&
                ( args.Length < 3  || !bool.TryParse(args[2], out var force) || !force))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-init-warning"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", args[0])));
            bool saveSuccess = _system.GetEntitySystem<MapLoaderSystem>().TrySaveMap(mapId, new ResPath(args[0]));
            if(saveSuccess)
            {
                    shell.WriteLine(Loc.GetString("cmd-savemap-success"));
            }
            else
            {
                    shell.WriteError(Loc.GetString("cmd-savemap-error"));
            }
        }
    }

    public sealed class LoadMap : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "loadmap";

        public static CompletionResult GetCompletionResult(IConsoleShell shell, string[] args, IResourceManager resource)
        {
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], resource.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[0], resource));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
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
            return GetCompletionResult(shell, args, _resource);
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 2 or > 6)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);

            //TODO make mapID optional and pick an unused number if unspecified

            if (!int.TryParse(args[1], out var intMapId))
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

            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadmap-exists", ("mapId", mapId)));
                return;
            }

            float x = 0;
            if (args.Length >= 3 && !float.TryParse(args[2], out x))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                return;
            }

            float y = 0;
            if (args.Length >= 4 && !float.TryParse(args[3], out y))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                return;
            }
            var offset = new Vector2(x, y);

            float rotation = 0;
            if (args.Length >= 5 && !float.TryParse(args[4], out rotation))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            var rot = new Angle(rotation);

            bool storeUids = false;
            if (args.Length >= 6 && !bool.TryParse(args[5], out storeUids))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                return;
            }

            var opts = new DeserializationOptions {StoreYamlUids = storeUids};

            _system.GetEntitySystem<MapLoaderSystem>().TryLoadMapWithId(mapId, path, out _, out _, opts, offset, rot);

            if (sys.MapExists(mapId))
                shell.WriteLine(Loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", path)));
            else
                shell.WriteLine(Loc.GetString("cmd-loadmap-error", ("path", path)));
        }
    }
}
