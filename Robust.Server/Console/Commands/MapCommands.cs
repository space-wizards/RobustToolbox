using System;
using System.Collections.Generic;
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
        [Dependency] private readonly IEntityManager _ent = null!;
        [Dependency] private readonly IResourceManager _resource = null!;
        [Dependency] private readonly IEntitySystemManager _system = null!;

        public override string Command => "savegrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Validate the number of parameters
            if (args.Length is < 1 or > 2)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);

            // Grid target can be omitted to automatically choose the grid currently under the player
            if (args.Length == 1)
                SaveGridCurrent(shell, path);
            else
                SaveGridSpecified(args[1], shell, path);
        }

        /// <summary>
        /// Save the grid currently under the player's attachedEntity
        /// </summary>
        private void SaveGridCurrent(IConsoleShell shell, ResPath path)
        {
            // Get the player's controlled entity
            var ent = shell.Player?.AttachedEntity;
            if (ent is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(Loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            var gridEnt = _system.GetEntitySystem<TransformSystem>().GetGrid(ent.Value);

            // Validate if the player is over a grid
            if (gridEnt is null)
            {
                shell.WriteLine(Help);
                shell.WriteError(Loc.GetString("cmd-savegrid-no-player-grid"));
                return;
            }

            SaveGrid(gridEnt.Value, gridEnt.Value.ToString(), shell, path);
        }

        /// <summary>
        /// Save a specific grid
        /// </summary>
        private void SaveGridSpecified(string targetGrid, IConsoleShell shell, ResPath path )
        {
            // Validate the mapId parameter's type
            if (!NetEntity.TryParse(targetGrid, out var gridNet))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-grid",("arg",targetGrid)));
                return;
            }

            var uid = _ent.GetEntity(gridNet);
            SaveGrid(uid, targetGrid, shell, path);
        }

        private void SaveGrid(EntityUid uid, string targetGrid, IConsoleShell shell, ResPath path)
        {
            // Validate if the entity being saved actually exists
            if (!_ent.EntityExists(uid))
            {
                shell.WriteError(Loc.GetString("cmd-savegrid-existnt",("uid",targetGrid)));
                return;
            }

            //  Validate if the targeted entity is a grid
            if (!_ent.HasComponent<MapGridComponent>(uid))
            {
                shell.WriteError(Loc.GetString("cmd-savegrid-not-grid",("uid", uid.ToString()),("ent", uid)));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-savegrid-attempt",("uid", uid.ToString())));
            var saveSuccess = _ent.System<MapLoaderSystem>().TrySaveGrid(uid, path);
            shell.WriteLine(saveSuccess
                ? Loc.GetString("cmd-savegrid-success")
                : Loc.GetString("cmd-savegrid-fail"));
        }

        // Parameter autocomplete and hints
        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.Components<MapGridComponent>(args[1], _ent),
                        Loc.GetString("cmd-hint-savegrid-id"));
            }
            return CompletionResult.Empty;
        }
    }

    public sealed class LoadGridCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = null!;
        [Dependency] private readonly IEntitySystemManager _system = null!;
        [Dependency] private readonly IResourceManager _resource = null!;

        public override string Command => "loadgrid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {

            // Validate the number of parameters
            if (args.Length is < 1 or > 6)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);

            // Target location parameters can be omitted to place the grid directly under the player
            if (args.Length == 1)
                LoadGridCurrent(shell, path);
            else
                LoadGriSpecific(args, shell, path);
        }

        /// <summary>
        /// Loads a saved grid from a path. It will appear under the player's current position
        /// </summary>
        private void LoadGridCurrent(IConsoleShell shell, ResPath path)
        {
            if (shell.Player?.AttachedEntity is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(Loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            // Read all the necessary information from the player's attached entity
            var ent = shell.Player.AttachedEntity.Value;
            var offset = _system.GetEntitySystem<TransformSystem>().GetWorldPosition(ent);
            var mapId = _system.GetEntitySystem<TransformSystem>().GetMapId(ent);
            var rot = _system.GetEntitySystem<TransformSystem>().GetWorldRotation(ent);
            var opts = DeserializationOptions.Default;

            LoadGrid(mapId, shell, path, opts, offset, rot, true);
        }

        /// <summary>
        /// Loads a saved grid from a path, to a specific map and position
        /// </summary>
        private void LoadGriSpecific(string[] args, IConsoleShell shell, ResPath path)
        {
            var opts = DeserializationOptions.Default;

            // Validate the mapId parameter's type
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", args[1])));
                return;
            }

            var mapId = new MapId(intMapId);
            // no loading into null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-loadgrid-nullspace-map"));
                return;
            }

            // Validate if the target map exists
            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (!sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadgrid-missing-map", ("mapId", mapId)));
                return;
            }

            // Validate the x coordinate's type
            var x = 0f;
            if (args.Length >= 3 && !float.TryParse(args[2], out x))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                return;
            }

            // Validate the y coordinate's type
            var y = 0f;
            if (args.Length >= 4 && !float.TryParse(args[3], out y))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                return;
            }
            var offset = new Vector2(x, y);

            // Validate the rotation parameter's type
            var rotation = 0f;
            if (args.Length >= 5 && !float.TryParse(args[4], out rotation))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            var rot = Angle.FromDegrees(rotation);

            // Validate the storeUid parameter's type
            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                    return;
                }

                opts.StoreYamlUids = storeUids;
            }

            LoadGrid(mapId, shell, path, opts, offset, rot, false);
        }
        private void LoadGrid(
            MapId mapId,
            IConsoleShell shell,
            ResPath path,
            DeserializationOptions opts,
            Vector2 offset,
            Angle rot,
            bool currentPos)
        {
            shell.WriteLine(currentPos
                    ? Loc.GetString("cmd-loadgrid-attempt-current")
                    : Loc.GetString("cmd-loadgrid-attempt", ("mapId", mapId)));

            var loadSuccess = _system.GetEntitySystem<MapLoaderSystem>()
                .TryLoadGrid(mapId, path, out _, opts, offset, rot);
            shell.WriteLine(loadSuccess
                ? Loc.GetString("cmd-loadgrid-success")
                : Loc.GetString("cmd-loadgrid-fail"));
        }

        // Parameter autocomplete and hints
        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 2:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.MapIds(_entManager),
                        Loc.GetString("cmd-hint-savemap-id"));
                default:
                    return LoadMap.GetCompletionResult(shell, args, _resource, _system);
            }
        }
    }

    public sealed class SaveMap : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = null!;
        [Dependency] private readonly IEntitySystemManager _system = null!;
        [Dependency] private readonly IResourceManager _resource = null!;

        public override string Command => "savemap";

        // Parameter autocomplete and hints
        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData);
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.MapIds(_entManager),
                        Loc.GetString("cmd-hint-savemap-id"));
                case 3:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.Booleans,
                        Loc.GetString("cmd-hint-savemap-force"));
            }
            return CompletionResult.Empty;
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Validate the number of parameters
            if (args.Length is < 1 or > 3 )
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);

            // Map target can be omitted to automatically choose the map the player is on
            if (args.Length == 1)
                SaveMapCurrent(shell, path);
            else
                SaveMapSpecified(args, shell, path);
        }

        private void SaveMapSpecified(string[] args, IConsoleShell shell, ResPath path)
        {
            // Validate the mapId parameter
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", args[1])));
                return;
            }
            var mapId = new MapId(intMapId);

            // Validate the force-save parameter
            var force = args.Length is 3 && bool.TryParse(args[2], out _);

            SaveMapDo(mapId, shell, path, force);
        }

        private void SaveMapCurrent(IConsoleShell shell, ResPath path)
        {
            // Get the player's controlled entity
            var ent = shell.Player?.AttachedEntity;

            if (ent is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(Loc.GetString("cmd-savemap-no-player-ent"));
                return;
            }

            var mapId = _system.GetEntitySystem<TransformSystem>().GetMapId(ent.Value);

            SaveMapDo(mapId, shell, path, false);
        }

        private void SaveMapDo(MapId mapId, IConsoleShell shell, ResPath path, bool force)
        {
            // no saving null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-savemap-nullspace"));
                return;
            }

            // Validate if the map to be saved exists
            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (!sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            // If the map to be saved is initialized, only allow saving it if the Force parameter was given
            if (sys.IsInitialized(mapId) && !force)
            {
                shell.WriteError(Loc.GetString("cmd-savemap-init-warning"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", path)));
            var saveSuccess = _system.GetEntitySystem<MapLoaderSystem>().TrySaveMap(mapId, path);

            shell.WriteLine(saveSuccess
                ? Loc.GetString("cmd-savemap-success")
                :Loc.GetString("cmd-savemap-error"));
        }
    }

    public sealed class LoadMap : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _system = null!;
        [Dependency] private readonly IResourceManager _resource = null!;

        public override string Command => "loadmap";

        // Parameter autocomplete and hints
        public static CompletionResult GetCompletionResult(IConsoleShell shell, string[] args, IResourceManager resource, IEntitySystemManager system)
        {
            List<CompletionOption> autocomplete;
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], resource.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[0], resource));
                    return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-id"));
                case 3:
                    GetPos(out var x, out _);
                    autocomplete = new List<CompletionOption>() {new (x.ToString())};
                    return CompletionResult.FromHintOptions(autocomplete , Loc.GetString("cmd-hint-loadmap-x-position"));
                case 4:
                    GetPos(out var _, out var y);
                    autocomplete = new List<CompletionOption>() {new (y.ToString())};
                    return CompletionResult.FromHintOptions(autocomplete, Loc.GetString("cmd-hint-loadmap-y-position"));
                case 5:
                    return CompletionResult.FromHint(Loc.GetString("cmd-hint-loadmap-rotation"));
                case 6:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.Booleans,
                        Loc.GetString("cmd-hint-loadmap-uids"));
            }

            return CompletionResult.Empty;

            // Get the player's location data for autocomplete suggestions
            void GetPos(out int x, out int y)
            {
                x = 0;
                y = 0;

                var ent = shell.Player?.AttachedEntity;
                if (ent is null)
                    return;

                var offset = system.GetEntitySystem<TransformSystem>().GetWorldPosition(ent.Value);
                x = (int)Math.Round(offset.X);
                y = (int)Math.Round(offset.Y);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return GetCompletionResult(shell, args, _resource, _system);
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            //Validate the number of parameters
            if (args.Length is < 1 or > 6)
            {
                shell.WriteLine(Help);
                return;
            }

            var path = new ResPath(args[0]);

            //  Make mapID optional and pick the next available number if unspecified
            if (args.Length is 1)
                LoadMapNext(shell, path);
            else
                LoadMapSpecified(args, shell, path);
        }

        private void LoadMapNext(IConsoleShell shell, ResPath path)
        {
            Vector2 offset = default;
            Angle rot = default;
            var opts = new DeserializationOptions {StoreYamlUids = false};

            shell.WriteLine(Loc.GetString("cmd-loadmap-attempt-next"));

            var loadSuccess = _system.GetEntitySystem<MapLoaderSystem>()
                .TryLoadMap(path, out var map, out _, opts, offset, rot);

            shell.WriteLine( loadSuccess && map is not null
                ? Loc.GetString("cmd-loadmap-success", ("mapId", map.Value.Comp.MapId), ("path", path))
                : Loc.GetString("cmd-loadmap-error", ("path", path)));
        }

        private void LoadMapSpecified(string[] args, IConsoleShell shell, ResPath path)
        {
            // Validate the mapId parameter's type
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[1])));
                return;
            }

            var mapId = new MapId(intMapId);

            // no loading null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-loadmap-nullspace"));
                return;
            }

            // Do not allow loading the map onto an already taken Id
            var sys = _system.GetEntitySystem<SharedMapSystem>();
            if (sys.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-loadmap-exists", ("mapId", mapId)));
                return;
            }

            // Validate the coordinate parameters' type
            var x = 0f;
            if (args.Length >= 3 && !float.TryParse(args[2], out x))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                return;
            }

            var y = 0f;
            if (args.Length >= 4 && !float.TryParse(args[3], out y))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                return;
            }
            var offset = new Vector2(x, y);

            // Validate the rotation parameter's type
            var rotation = 0f;
            if (args.Length >= 5 && !float.TryParse(args[4], out rotation))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            var rot = new Angle(rotation);

            // Validate the storeUid parameter's type
            var storeUids = false;
            if (args.Length >= 6 && !bool.TryParse(args[5], out storeUids))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                return;
            }

            var opts = new DeserializationOptions {StoreYamlUids = storeUids};

            shell.WriteLine(Loc.GetString("cmd-loadmap-attempt",("mapId", mapId)));

            var loadSuccess = _system.GetEntitySystem<MapLoaderSystem>()
                .TryLoadMapWithId(mapId, path, out _, out _, opts, offset, rot);

            shell.WriteLine( loadSuccess
                ? Loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", path))
                : Loc.GetString("cmd-loadmap-error", ("path", path)));
        }
    }
}
