using System;
using System.Collections.Generic;
using System.Linq;
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
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IEntityManager _entityManager;

        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new HashSet<MapId>();
        [ViewVariables] private readonly HashSet<GridId> _initializedGrids = new HashSet<GridId>();

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

        public void MapInitializeGrid(GridId gridId)
        {
            MapInitializeGrid(_mapManager.GetGrid(gridId));
        }

        public void MapInitializeGrid(IMapGrid grid)
        {
            if (_initializedGrids.Contains(grid.Index))
            {
                throw new ArgumentException("That grid is already initialized", nameof(grid));
            }

            _initializedGrids.Add(grid.Index);

            foreach (var entity in _entityManager.GetEntities())
            {
                if (entity.Transform.GridID != grid.Index)
                {
                    continue;
                }

                entity.RunMapInit();
            }
        }

        public bool IsMapPaused(IMap map) => IsMapPaused(map.Index);
        public bool IsMapPaused(MapId mapId) => _pausedMaps.Contains(mapId);
        public bool IsGridPaused(IMapGrid grid) => _pausedMaps.Contains(grid.ParentMapId);

        public bool IsGridPaused(GridId gridId)
        {
            var grid = _mapManager.GetGrid(gridId);
            return IsGridPaused(grid);
        }

        public bool IsGridMapInitialized(GridId gridId)
        {
            return _initializedGrids.Contains(gridId);
        }

        public bool IsGridMapInitialized(IMapGrid grid)
        {
            return IsGridMapInitialized(grid.Index);
        }

        public void PostInject()
        {
            _mapManager.MapDestroyed += (sender, args) => _pausedMaps.Remove(args.Map.Index);
        }
    }
}
