using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Timing
{
    internal sealed class PauseManager : IPauseManager, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IEntityManager _entityManager;
#pragma warning restore 649

        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new HashSet<MapId>();
        [ViewVariables] private readonly HashSet<MapId> _unInitializedMaps = new HashSet<MapId>();

        public void SetMapPaused(IMap map, bool paused) => SetMapPaused(map.Index, paused);
        public void SetMapPaused(MapId mapId, bool paused)
        {
            if (paused)
            {
                _pausedMaps.Add(mapId);
            }
            else
            {
                _pausedMaps.Remove(mapId);
            }
        }

        public void DoMapInitialize(IMap map) => DoMapInitialize(map.Index);
        public void DoMapInitialize(MapId mapId)
        {
            if (IsMapInitialized(mapId))
            {
                throw new ArgumentException("That map is already initialized.");
            }

            _unInitializedMaps.Remove(mapId);

            foreach (var entity in _entityManager.GetEntities())
            {
                if (entity.Transform.MapID != mapId)
                {
                    continue;
                }

                entity.RunMapInit();
            }
        }

        public void DoGridMapInitialize(IMapGrid grid) => DoGridMapInitialize(grid.Index);
        public void DoGridMapInitialize(GridId gridId)
        {
            foreach (var entity in _entityManager.GetEntities())
            {
                if (entity.Transform.GridID != gridId)
                {
                    continue;
                }

                entity.RunMapInit();
            }
        }

        public void AddUninitializedMap(IMap map) => AddUninitializedMap(map.Index);
        public void AddUninitializedMap(MapId mapId)
        {
            _unInitializedMaps.Add(mapId);
        }

        public bool IsMapPaused(IMap map) => IsMapPaused(map.Index);
        public bool IsMapPaused(MapId mapId) => _pausedMaps.Contains(mapId) || _unInitializedMaps.Contains(mapId);
        public bool IsGridPaused(IMapGrid grid) => IsMapPaused(grid.ParentMapId);

        public bool IsGridPaused(GridId gridId)
        {
            var grid = _mapManager.GetGrid(gridId);
            return IsGridPaused(grid);
        }

        public bool IsMapInitialized(IMap map) => IsMapInitialized(map.Index);
        public bool IsMapInitialized(MapId mapId)
        {
            return !_unInitializedMaps.Contains(mapId);
        }

        public void PostInject()
        {
            _mapManager.MapDestroyed += (sender, args) =>
            {
                _pausedMaps.Remove(args.Map.Index);
                _unInitializedMaps.Add(args.Map.Index);
            };
        }
    }
}
