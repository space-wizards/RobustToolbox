using System;
using System.Collections.Generic;
using System.Globalization;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Timing
{
    internal sealed class PauseManager : IPauseManager, IPostInjectInit
    {
        [Dependency] private readonly IConsoleHost _conhost = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new();
        [ViewVariables] private readonly HashSet<MapId> _unInitializedMaps = new();

        public void SetMapPaused(MapId mapId, bool paused)
        {
            var lookupSystem = IoCManager.Resolve<IEntityLookup>();

            if (paused)
            {
                _pausedMaps.Add(mapId);

                foreach (var entity in lookupSystem.GetEntitiesInMap(mapId))
                {
                    entity.Paused = true;
                }
            }
            else
            {
                _pausedMaps.Remove(mapId);

                foreach (var entity in lookupSystem.GetEntitiesInMap(mapId))
                {
                    entity.Paused = false;
                }
            }
        }

        public void DoMapInitialize(MapId mapId)
        {
            if (IsMapInitialized(mapId))
                throw new ArgumentException("That map is already initialized.");

            _unInitializedMaps.Remove(mapId);

            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInMap(mapId))
            {
                entity.RunMapInit();
                entity.Paused = false;
            }
        }

        public void DoGridMapInitialize(IMapGrid grid)
        {
            DoGridMapInitialize(grid.Index);
        }

        public void DoGridMapInitialize(GridId gridId)
        {
            var mapId = _mapManager.GetGrid(gridId).ParentMapId;

            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInMap(mapId))
            {
                if (entity.Transform.GridID != gridId)
                    continue;

                entity.RunMapInit();
                entity.Paused = false;
            }
        }

        public void AddUninitializedMap(MapId mapId)
        {
            _unInitializedMaps.Add(mapId);
        }

        public bool IsMapPaused(MapId mapId)
        {
            return _pausedMaps.Contains(mapId) || _unInitializedMaps.Contains(mapId);
        }

        public bool IsGridPaused(IMapGrid grid)
        {
            return IsMapPaused(grid.ParentMapId);
        }

        public bool IsGridPaused(GridId gridId)
        {
            if (_mapManager.TryGetGrid(gridId, out var grid))
            {
                return IsGridPaused(grid);
            }

            Logger.ErrorS("map", $"Tried to check if unknown grid {gridId} was paused.");
            return true;
        }

        public bool IsMapInitialized(MapId mapId)
        {
            return !_unInitializedMaps.Contains(mapId);
        }

        /// <inheritdoc />
        public void PostInject()
        {
            _mapManager.MapDestroyed += (_, args) =>
            {
                _pausedMaps.Remove(args.Map);
                _unInitializedMaps.Add(args.Map);
            };

            //if(_conhost.IsClient)
            //  return;

            _conhost.RegisterCommand("pausemap",
                "Pauses a map, pausing all simulation processing on it.",
                "pausemap <map ID>",
                (shell, _, args) =>
                {
                    if (args.Length != 1)
                    {
                        shell.WriteError("Need to supply a valid MapId");
                        return;
                    }

                    string? arg = args[0];
                    var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

                    if (!_mapManager.MapExists(mapId))
                    {
                        shell.WriteError("That map does not exist.");
                        return;
                    }

                    SetMapPaused(mapId, true);
                });

            _conhost.RegisterCommand("querymappaused",
                "Check whether a map is paused or not.",
                "querymappaused <map ID>",
                (shell, _, args) =>
                {
                    string? arg = args[0];
                    var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

                    if (!_mapManager.MapExists(mapId))
                    {
                        shell.WriteError("That map does not exist.");
                        return;
                    }

                    shell.WriteLine(IsMapPaused(mapId).ToString());
                });

            _conhost.RegisterCommand("unpausemap",
                "unpauses a map, resuming all simulation processing on it.",
                "Usage: unpausemap <map ID>",
                (shell, _, args) =>
                {
                    if (args.Length != 1)
                    {
                        shell.WriteLine("Need to supply a valid MapId");
                        return;
                    }

                    string? arg = args[0];
                    var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

                    if (!_mapManager.MapExists(mapId))
                    {
                        shell.WriteLine("That map does not exist.");
                        return;
                    }

                    SetMapPaused(mapId, false);
                });
        }
    }
}
