using System.Linq;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Server.Console.Commands
{
    public sealed class SaveGridCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _ent = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "savegrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteError("Not enough arguments.");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet))
            {
                shell.WriteError("Not a valid entity ID.");
                return;
            }

            var uid = _ent.GetEntity(uidNet);

            // no saving default grid
            if (!_ent.EntityExists(uid))
            {
                shell.WriteError("That grid does not exist.");
                return;
            }

            _ent.System<MapLoaderSystem>().Save(uid, args[1]);
            shell.WriteLine("Save successful. Look in the user data directory.");
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savebp-id"));
                case 2:
                    var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
            }
            return CompletionResult.Empty;
        }
    }

    public sealed class LoadGridCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

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

            if (!_map.MapExists(mapId))
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

            _system.GetEntitySystem<MapLoaderSystem>().Load(mapId, args[1], loadOptions);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return LoadMap.GetCompletionResult(shell, args, _resource);
        }
    }

    public sealed class SaveMap : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "savemap";

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 2:
                    var opts = CompletionHelper.UserFilePath(args[1], _resource.UserData);
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

            if (!_map.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            if (_map.IsMapInitialized(mapId) &&
                ( args.Length < 3  || !bool.TryParse(args[2], out var force) || !force))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-init-warning"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", args[1])));
            _system.GetEntitySystem<MapLoaderSystem>().SaveMap(mapId, args[1]);
            shell.WriteLine(Loc.GetString("cmd-savemap-success"));
        }
    }

    public sealed class LoadMap : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly IResourceManager _resource = default!;

        public override string Command => "loadmap";

        public static CompletionResult GetCompletionResult(IConsoleShell shell, string[] args, IResourceManager resource)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 2:
                    var opts = CompletionHelper.UserFilePath(args[1], resource.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[1], resource));
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
            return GetCompletionResult(shell, args, _resource);
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

            if (_map.MapExists(mapId))
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

            _system.GetEntitySystem<MapLoaderSystem>().TryLoad(mapId, args[1], out _, loadOptions);

            if (_map.MapExists(mapId))
                shell.WriteLine(Loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", args[1])));
            else
                shell.WriteLine(Loc.GetString("cmd-loadmap-error", ("path", args[1])));
        }
    }
}
