using System.Collections.Generic;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Server.Timing
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
