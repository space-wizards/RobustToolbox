using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map
{
    internal partial class MapManager
    {
        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new();
        [ViewVariables] private readonly HashSet<MapId> _unInitializedMaps = new();

        /// <inheritdoc />
        public void SetMapPaused(MapId mapId, bool paused)
        {
            if (paused)
            {
                _pausedMaps.Add(mapId);

                foreach (var entity in _entityLookup.GetEntitiesInMap(mapId))
                {
                    EntityManager.GetComponent<MetaDataComponent>(entity).EntityPaused = true;
                }
            }
            else
            {
                _pausedMaps.Remove(mapId);

                foreach (var entity in _entityLookup.GetEntitiesInMap(mapId))
                {
                    EntityManager.GetComponent<MetaDataComponent>(entity).EntityPaused = false;
                }
            }
        }

        /// <inheritdoc />
        public void DoMapInitialize(MapId mapId)
        {
            if (IsMapInitialized(mapId))
                throw new ArgumentException("That map is already initialized.");

            _unInitializedMaps.Remove(mapId);

            foreach (var entity in _entityLookup.GetEntitiesInMap(mapId).ToArray())
            {
                entity.RunMapInit();

                // MapInit could have deleted this entity.
                if(EntityManager.TryGetComponent(entity, out MetaDataComponent? meta))
                    meta.EntityPaused = false;
            }
        }

        /// <inheritdoc />
        public void DoGridMapInitialize(IMapGrid grid)
        {
            DoGridMapInitialize(grid.Index);
        }

        /// <inheritdoc />
        public void DoGridMapInitialize(GridId gridId)
        {
            var mapId = GetGrid(gridId).ParentMapId;

            foreach (var entity in _entityLookup.GetEntitiesInMap(mapId))
            {
                if (EntityManager.GetComponent<TransformComponent>(entity).GridID != gridId)
                    continue;

                entity.RunMapInit();
                EntityManager.GetComponent<MetaDataComponent>(entity).EntityPaused = false;
            }
        }

        /// <inheritdoc />
        public void AddUninitializedMap(MapId mapId)
        {
            _unInitializedMaps.Add(mapId);
        }

        /// <inheritdoc />
        public bool IsMapPaused(MapId mapId)
        {
            return _pausedMaps.Contains(mapId) || _unInitializedMaps.Contains(mapId);
        }

        /// <inheritdoc />
        public bool IsGridPaused(IMapGrid grid)
        {
            return IsMapPaused(grid.ParentMapId);
        }

        /// <inheritdoc />
        public bool IsGridPaused(GridId gridId)
        {
            if (TryGetGrid(gridId, out var grid))
            {
                return IsGridPaused(grid);
            }

            Logger.ErrorS("map", $"Tried to check if unknown grid {gridId} was paused.");
            return true;
        }

        /// <inheritdoc />
        public bool IsMapInitialized(MapId mapId)
        {
            return !_unInitializedMaps.Contains(mapId);
        }

        /// <summary>
        /// Initializes the map pausing system.
        /// </summary>
        private void InitializeMapPausing()
        {
            MapDestroyed += (_, args) =>
            {
                _pausedMaps.Remove(args.Map);
                _unInitializedMaps.Add(args.Map);
            };

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

                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
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
                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
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

                    var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

                    if (!MapExists(mapId))
                    {
                        shell.WriteLine("That map does not exist.");
                        return;
                    }

                    SetMapPaused(mapId, false);
                });
        }
    }
}
