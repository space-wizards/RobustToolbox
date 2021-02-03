using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Timing
{
    internal sealed class PauseManager : IPauseManager, IPostInjectInit
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new();
        [ViewVariables] private readonly HashSet<MapId> _unInitializedMaps = new();

        public void SetMapPaused(MapId mapId, bool paused)
        {
            if (paused)
            {
                _pausedMaps.Add(mapId);
                foreach (var entity in _entityManager.GetEntitiesInMap(mapId))
                {
                    entity.Paused = true;
                }
            }
            else
            {
                _pausedMaps.Remove(mapId);
                foreach (var entity in _entityManager.GetEntitiesInMap(mapId))
                {
                    entity.Paused = false;
                }
            }
        }

        public void DoMapInitialize(MapId mapId)
        {
            if (IsMapInitialized(mapId))
            {
                throw new ArgumentException("That map is already initialized.");
            }

            _unInitializedMaps.Remove(mapId);

            foreach (var entity in _entityManager.GetEntitiesInMap(mapId))
            {
                entity.RunMapInit();
                entity.Paused = false;
            }
        }

        public void DoGridMapInitialize(IMapGrid grid) => DoGridMapInitialize(grid.Index);
        public void DoGridMapInitialize(GridId gridId)
        {
            var mapId = _mapManager.GetGrid(gridId).ParentMapId;

            foreach (var entity in _entityManager.GetEntitiesInMap(mapId))
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

        public bool IsMapPaused(MapId mapId) => _pausedMaps.Contains(mapId) || _unInitializedMaps.Contains(mapId);
        public bool IsGridPaused(IMapGrid grid) => IsMapPaused(grid.ParentMapId);

        public bool IsGridPaused(GridId gridId)
        {
            var grid = _mapManager.GetGrid(gridId);
            return IsGridPaused(grid);
        }

        public bool IsMapInitialized(MapId mapId)
        {
            return !_unInitializedMaps.Contains(mapId);
        }

        public void PostInject()
        {
            _mapManager.MapDestroyed += (sender, args) =>
            {
                _pausedMaps.Remove(args.Map);
                _unInitializedMaps.Add(args.Map);
            };
        }
    }
}
