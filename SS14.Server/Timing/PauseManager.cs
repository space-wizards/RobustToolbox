using System.Collections.Generic;
using SS14.Server.Interfaces.Timing;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.ViewVariables;

namespace SS14.Server.Timing
{
    public class PauseManager : IPauseManager, IPostInjectInit
    {
        [Dependency] private IMapManager _mapManager;

        [ViewVariables] private readonly HashSet<MapId> _pausedMaps = new HashSet<MapId>();

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

        public bool IsMapPaused(IMap map) => IsMapPaused(map.Index);
        public bool IsMapPaused(MapId mapId) => _pausedMaps.Contains(mapId);
        public bool IsGridPaused(IMapGrid grid) => _pausedMaps.Contains(grid.MapID);

        public bool IsGridPaused(GridId gridId)
        {
            var grid = _mapManager.GetGrid(gridId);
            return IsGridPaused(grid);
        }

        public void PostInject()
        {
            _mapManager.MapDestroyed += (sender, args) => _pausedMaps.Remove(args.Map.Index);
        }
    }
}
