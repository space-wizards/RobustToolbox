using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    public sealed partial class SaveGridCommand : LocalizedEntityCommands
    {
        [Dependency] private IEntityManager _ent = default!;
        [Dependency] private IResourceManager _resource = default!;
        [Dependency] private SharedTransformSystem _trans = default!;

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
                SaveGridCurrent(shell, path, Loc);
            else
                SaveGridSpecified(args[1], shell, path, Loc);
        }

        /// <summary>
        /// Save the grid currently under the player's attachedEntity
        /// </summary>
        private void SaveGridCurrent(IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Get the player's controlled entity
            var ent = shell.Player?.AttachedEntity;
            if (ent is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            var gridEnt = _trans.GetGrid(ent.Value);

            // Validate if the player is over a grid
            if (gridEnt is null)
            {
                shell.WriteLine(Help);
                shell.WriteError(loc.GetString("cmd-savegrid-no-player-grid"));
                return;
            }

            SaveGrid(gridEnt.Value, gridEnt.Value.ToString(), shell, path, loc);
        }

        /// <summary>
        /// Save a specific grid
        /// </summary>
        private void SaveGridSpecified(string targetGrid, IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Validate the mapId parameter's type
            if (!NetEntity.TryParse(targetGrid, out var gridNet))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-grid",("arg",targetGrid)));
                return;
            }

            var uid = _ent.GetEntity(gridNet);
            SaveGrid(uid, targetGrid, shell, path, loc);
        }

        private void SaveGrid(EntityUid uid, string targetGrid, IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Validate if the entity being saved actually exists
            if (!_ent.EntityExists(uid))
            {
                shell.WriteError(loc.GetString("cmd-savegrid-existnt",("uid",targetGrid)));
                return;
            }

            //  Validate if the targeted entity is a grid
            if (!_ent.HasComponent<MapGridComponent>(uid))
            {
                shell.WriteError(loc.GetString("cmd-savegrid-not-grid",("uid", uid.ToString()),("ent", uid)));
                return;
            }

            shell.WriteLine(loc.GetString("cmd-savegrid-attempt",("uid", uid.ToString())));
            var saveSuccess = _ent.System<MapLoaderSystem>().TrySaveGrid(uid, path);
            shell.WriteLine(saveSuccess
                ? loc.GetString("cmd-savegrid-success")
                : loc.GetString("cmd-savegrid-fail"));
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

    public sealed partial class LoadGridCommand : LocalizedEntityCommands
    {
        [Dependency] private IEntityManager _entManager = default!;
        [Dependency] private SharedMapSystem _map = default!;
        [Dependency] private MapLoaderSystem _mapLoader = default!;
        [Dependency] private IResourceManager _resource = default!;
        [Dependency] private SharedTransformSystem _trans = default!;

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
                LoadGridCurrent(shell, path, Loc);
            else
                LoadGriSpecific(args, shell, path, Loc);
        }

        /// <summary>
        /// Loads a saved grid from a path. It will appear under the player's current position
        /// </summary>
        private void LoadGridCurrent(IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            if (shell.Player?.AttachedEntity is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            // Read all the necessary information from the player's attached entity
            var ent = shell.Player.AttachedEntity.Value;
            var offset = _trans.GetWorldPosition(ent);
            var mapId = _trans.GetMapId(ent);
            var rot = _trans.GetWorldRotation(ent);
            var opts = DeserializationOptions.Default;

            LoadGrid(mapId, shell, path, opts, offset, rot, true, loc);
        }

        /// <summary>
        /// Loads a saved grid from a path, to a specific map and position
        /// </summary>
        private void LoadGriSpecific(string[] args, IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            var opts = DeserializationOptions.Default;

            // Validate the mapId parameter's type
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-mapid", ("arg", args[1])));
                return;
            }

            var mapId = new MapId(intMapId);
            // no loading into null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(loc.GetString("cmd-loadgrid-nullspace-map"));
                return;
            }

            // Validate if the target map exists
            if (!_map.MapExists(mapId))
            {
                shell.WriteError(loc.GetString("cmd-loadgrid-missing-map", ("mapId", mapId)));
                _map.CreateMap(mapId, false); // doesnt runmapinit to be conservative.
            }

            // Validate the x coordinate's type
            var x = 0f;
            if (args.Length >= 3 && !float.TryParse(args[2], out x))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                return;
            }

            // Validate the y coordinate's type
            var y = 0f;
            if (args.Length >= 4 && !float.TryParse(args[3], out y))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                return;
            }
            var offset = new Vector2(x, y);

            // Validate the rotation parameter's type
            var rotation = 0f;
            if (args.Length >= 5 && !float.TryParse(args[4], out rotation))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            var rot = Angle.FromDegrees(rotation);

            // Validate the storeUid parameter's type
            if (args.Length >= 6)
            {
                if (!bool.TryParse(args[5], out var storeUids))
                {
                    shell.WriteError(loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                    return;
                }

                opts.StoreYamlUids = storeUids;
            }

            LoadGrid(mapId, shell, path, opts, offset, rot, false, loc);
        }
        private void LoadGrid(
            MapId mapId,
            IConsoleShell shell,
            ResPath path,
            DeserializationOptions opts,
            Vector2 offset,
            Angle rot,
            bool currentPos,
            ILocalizationManager loc)
        {
            shell.WriteLine(currentPos
                    ? loc.GetString("cmd-loadgrid-attempt-current")
                    : loc.GetString("cmd-loadgrid-attempt", ("mapId", mapId)));

            var loadSuccess = _mapLoader.TryLoadGrid(mapId, path, out _, opts, offset, rot);
            shell.WriteLine(loadSuccess
                ? loc.GetString("cmd-loadgrid-success")
                : loc.GetString("cmd-loadgrid-fail"));
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
                    return LoadMap.GetCompletionResult(shell, args, _resource, _trans, Loc);
            }

        }
    }

    public sealed partial class SaveMap : LocalizedEntityCommands
    {
        [Dependency] private IEntityManager _entManager = default!;
        [Dependency] private SharedMapSystem _map = default!;
        [Dependency] private MapLoaderSystem _mapLoader = default!;
        [Dependency] private IResourceManager _resource = default!;
        [Dependency] private SharedTransformSystem _trans = default!;

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
                SaveMapCurrent(shell, path, Loc);
            else
                SaveMapSpecified(args, shell, path, Loc);
        }

        private void SaveMapSpecified(string[] args, IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Validate the mapId parameter
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-mapid", ("arg", args[1])));
                return;
            }
            var mapId = new MapId(intMapId);

            // Validate the force-save parameter
            var force = args.Length is 3 && bool.TryParse(args[2], out _);

            SaveMapDo(mapId, shell, path, force, loc);
        }

        private void SaveMapCurrent(IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Get the player's controlled entity
            var ent = shell.Player?.AttachedEntity;

            if (ent is null)
            {
                // We can't continue with just the first parameter without a player entity.
                // For example, if a server system ran this command.
                shell.WriteError(loc.GetString("cmd-savemap-no-player-ent"));
                return;
            }

            var mapId = _trans.GetMapId(ent.Value);

            SaveMapDo(mapId, shell, path, false, loc);
        }

        private void SaveMapDo(MapId mapId, IConsoleShell shell, ResPath path, bool force, ILocalizationManager loc)
        {
            // no saving null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(loc.GetString("cmd-savemap-nullspace"));
                return;
            }

            // Validate if the map to be saved exists
            if (!_map.MapExists(mapId))
            {
                shell.WriteError(loc.GetString("cmd-savemap-not-exist"));
                return;
            }

            // If the map to be saved is initialized, only allow saving it if the Force parameter was given
            if (_map.IsInitialized(mapId) && !force)
            {
                shell.WriteError(loc.GetString("cmd-savemap-init-warning"));
                return;
            }

            shell.WriteLine(loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", path)));
            var saveSuccess = _mapLoader.TrySaveMap(mapId, path);

            shell.WriteLine(saveSuccess
                ? loc.GetString("cmd-savemap-success")
                :loc.GetString("cmd-savemap-error"));
        }
    }

    public sealed partial class LoadMap : LocalizedEntityCommands
    {
        [Dependency] private SharedMapSystem _map = default!;
        [Dependency] private MapLoaderSystem _mapLoader = default!;
        [Dependency] private IResourceManager _resource = default!;
        [Dependency] private SharedTransformSystem _trans = default!;

        public override string Command => "loadmap";

        // Parameter autocomplete and hints
        public static CompletionResult GetCompletionResult(
            IConsoleShell shell,
            string[] args,
            IResourceManager resource,
            SharedTransformSystem trans,
            ILocalizationManager loc)
        {
            List<CompletionOption> autocomplete;
            switch (args.Length)
            {
                case 1:
                    var opts = CompletionHelper.UserFilePath(args[0], resource.UserData)
                        .Concat(CompletionHelper.ContentFilePath(args[0], resource));
                    return CompletionResult.FromHintOptions(opts, loc.GetString("cmd-hint-savemap-path"));
                case 2:
                    return CompletionResult.FromHint(loc.GetString("cmd-hint-savemap-id"));
                case 3:
                    GetPos(out var x, out _);
                    autocomplete = new List<CompletionOption>() {new (x.ToString())};
                    return CompletionResult.FromHintOptions(autocomplete , loc.GetString("cmd-hint-loadmap-x-position"));
                case 4:
                    GetPos(out var _, out var y);
                    autocomplete = new List<CompletionOption>() {new (y.ToString())};
                    return CompletionResult.FromHintOptions(autocomplete, loc.GetString("cmd-hint-loadmap-y-position"));
                case 5:
                    return CompletionResult.FromHint(loc.GetString("cmd-hint-loadmap-rotation"));
                case 6:
                    return CompletionResult.FromHintOptions(
                        CompletionHelper.Booleans,
                        loc.GetString("cmd-hint-loadmap-uids"));
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

                var offset = trans.GetWorldPosition(ent.Value);
                x = (int)Math.Round(offset.X);
                y = (int)Math.Round(offset.Y);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return GetCompletionResult(shell, args, _resource, _trans, Loc);
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
                LoadMapNext(shell, path, Loc);
            else
                LoadMapSpecified(args, shell, path, Loc);
        }

        private void LoadMapNext(IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            Vector2 offset = default;
            Angle rot = default;
            var opts = new DeserializationOptions {StoreYamlUids = false};

            shell.WriteLine(loc.GetString("cmd-loadmap-attempt-next"));

            var loadSuccess = _mapLoader.TryLoadMap(path, out var map, out _, opts, offset, rot);

            shell.WriteLine( loadSuccess && map is not null
                ? loc.GetString("cmd-loadmap-success", ("mapId", map.Value.Comp.MapId), ("path", path))
                : loc.GetString("cmd-loadmap-error", ("path", path)));
        }

        private void LoadMapSpecified(string[] args, IConsoleShell shell, ResPath path, ILocalizationManager loc)
        {
            // Validate the mapId parameter's type
            if (!int.TryParse(args[1], out var intMapId))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-integer", ("arg", args[1])));
                return;
            }

            var mapId = new MapId(intMapId);

            // no loading null space
            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(loc.GetString("cmd-loadmap-nullspace"));
                return;
            }

            // Do not allow loading the map onto an already taken Id
            if (_map.MapExists(mapId))
            {
                shell.WriteError(loc.GetString("cmd-loadmap-exists", ("mapId", mapId)));
                return;
            }

            // Validate the coordinate parameters' type
            var x = 0f;
            if (args.Length >= 3 && !float.TryParse(args[2], out x))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[2])));
                return;
            }

            var y = 0f;
            if (args.Length >= 4 && !float.TryParse(args[3], out y))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[3])));
                return;
            }
            var offset = new Vector2(x, y);

            // Validate the rotation parameter's type
            var rotation = 0f;
            if (args.Length >= 5 && !float.TryParse(args[4], out rotation))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-float", ("arg", args[4])));
                return;
            }
            var rot = new Angle(rotation);

            // Validate the storeUid parameter's type
            var storeUids = false;
            if (args.Length >= 6 && !bool.TryParse(args[5], out storeUids))
            {
                shell.WriteError(loc.GetString("cmd-parse-failure-bool", ("arg", args[5])));
                return;
            }

            var opts = new DeserializationOptions { StoreYamlUids = storeUids };

            shell.WriteLine(loc.GetString("cmd-loadmap-attempt",("mapId", mapId)));

            var loadSuccess = _mapLoader.TryLoadMapWithId(mapId, path, out _, out _, opts, offset, rot);

            shell.WriteLine( loadSuccess
                ? loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", path))
                : loc.GetString("cmd-loadmap-error", ("path", path)));
        }
    }
}
