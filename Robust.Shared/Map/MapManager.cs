using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <inheritdoc cref="IMapManager"/>
    public partial class MapManager : IMapManagerInternal, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        public IGameTiming GameTiming => _gameTiming;

        /// <inheritdoc />
        public MapId DefaultMap => MapId.Nullspace;

        /// <inheritdoc />
        public event EventHandler<TileChangedEventArgs> TileChanged;

        public event GridEventHandler OnGridCreated;

        public event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        /// <inheritdoc />
        public event EventHandler<GridChangedEventArgs> GridChanged;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapCreated;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapDestroyed;

        /// <inheritdoc />
        public bool SuppressOnTileChanged { get; set; }

        private MapId HighestMapID = MapId.Nullspace;
        private GridId HighestGridID = GridId.Nullspace;

        private readonly HashSet<MapId> _maps = new HashSet<MapId>();
        private readonly Dictionary<MapId, GameTick> _mapCreationTick = new Dictionary<MapId, GameTick>();

        private readonly Dictionary<GridId, MapGrid> _grids = new Dictionary<GridId, MapGrid>();
        private readonly Dictionary<MapId, GridId> _defaultGrids = new Dictionary<MapId, GridId>();

        private readonly List<(GameTick tick, GridId gridId)> _gridDeletionHistory = new List<(GameTick, GridId)>();
        private readonly List<(GameTick tick, MapId mapId)> _mapDeletionHistory = new List<(GameTick, MapId)>();

        public void PostInject()
        {
        }

        /// <inheritdoc />
        public void Initialize()
        {
            CreateMap(MapId.Nullspace, GridId.Nullspace);
            // So uh I removed the contents from this but I'm too lazy to remove the Initialize method.
            // Deal with it.
        }

        public void Startup()
        {
            // Ditto, removed contents but too lazy to remove method.
        }

        public void Shutdown()
        {
            foreach (var map in _maps.ToArray())
            {
                if (map != MapId.Nullspace)
                {
                    DeleteMap(map);
                }
            }

            DebugTools.Assert(_grids.Count == 1);
        }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        public void RaiseOnTileChanged(TileRef tileRef, Tile oldTile)
        {
            if (SuppressOnTileChanged)
                return;

            TileChanged?.Invoke(this, new TileChangedEventArgs(tileRef, oldTile));
        }

        /// <inheritdoc />
        public void DeleteMap(MapId mapID)
        {
            if (mapID == MapId.Nullspace)
            {
                Logger.DebugS("map", "Blocked deletion of nullspace map.");
                return;
            }

            if (!_maps.Contains(mapID))
            {
                throw new InvalidOperationException($"Attempted to delete nonexistant map '{mapID}'");
            }

            // grids are cached because Delete modifies collection
            foreach (var grid in GetAllMapGrids(mapID).ToList())
            {
                DeleteGrid(grid.Index);
            }

            MapDestroyed?.Invoke(this, new MapEventArgs(mapID));
            _maps.Remove(mapID);
            _mapCreationTick.Remove(mapID);

            if (_netManager.IsClient)
                return;

            _mapDeletionHistory.Add((_gameTiming.CurTick, mapID));
        }

        /// <inheritdoc />
        public MapId CreateMap(MapId? mapID = null, GridId? defaultGridID = null)
        {
            if (defaultGridID != null && GridExists(defaultGridID.Value))
            {
                throw new InvalidOperationException($"Grid '{defaultGridID}' already exists.");
            }
            MapId actualID;
            if (mapID != null)
            {
                actualID = mapID.Value;
            }
            else
            {
                actualID = new MapId(HighestMapID.Value + 1);
            }

            if (MapExists(actualID))
            {
                throw new InvalidOperationException($"A map with ID {actualID} already exists");
            }

            if (HighestMapID.Value < actualID.Value)
            {
                HighestMapID = actualID;
            }

            _maps.Add(actualID);
            _mapCreationTick.Add(actualID, _gameTiming.CurTick);
            Logger.InfoS("map", $"Creating new map {actualID}");
            MapCreated?.Invoke(this, new MapEventArgs(actualID));
            var newDefaultGrid = CreateGrid(actualID, defaultGridID);
            _defaultGrids.Add(actualID, newDefaultGrid.Index);

            return actualID;
        }

        /// <inheritdoc />
        public bool MapExists(MapId mapID)
        {
            return _maps.Contains(mapID);
        }

        public IEnumerable<MapId> GetAllMapIds()
        {
            return _maps;
        }

        public IMapGrid GetDefaultGrid(MapId mapID)
        {
            return _grids[_defaultGrids[mapID]];
        }

        public GridId GetDefaultGridId(MapId mapID)
        {
            return _defaultGrids[mapID];
        }

        public IEnumerable<IMapGrid> GetAllGrids()
        {
            return _grids.Values;
        }

        public IMapGrid CreateGrid(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16, float snapSize = 1)
        {
            GridId actualID;
            if (gridID != null)
            {
                actualID = gridID.Value;
            }
            else
            {
                actualID = new GridId(HighestGridID.Value + 1);
            }

            if (GridExists(actualID))
            {
                throw new InvalidOperationException($"A map with ID {actualID} already exists");
            }

            if (HighestGridID.Value < actualID.Value)
            {
                HighestGridID = actualID;
            }

            var grid = new MapGrid(this, actualID, chunkSize, snapSize, currentMapID);
            _grids.Add(actualID, grid);
            Logger.DebugS("map", $"Creating new grid {actualID}");

            if(actualID != GridId.Nullspace) // nullspace default grid is not bound to an entity
            {
                // the entity may already exist from map deserialization
                IMapGridComponent result = null;
                foreach (var comp in _entityManager.ComponentManager.GetAllComponents<IMapGridComponent>())
                {
                    if (comp.GridIndex != actualID)
                        continue;

                    result = comp;
                    break;
                }

                if (result != null)
                {
                    grid.GridEntity = result.Owner.Uid;
                    Logger.DebugS("map", $"Rebinding grid {actualID} to entity {grid.GridEntity}");
                }
                else
                {
                    var newEnt = _entityManager.SpawnEntity(null, new GridCoordinates(0, 0, actualID));
                    grid.GridEntity = newEnt.Uid;

                    var gridComp = newEnt.AddComponent<MapGridComponent>();
                    gridComp.GridIndex = grid.Index;
                    Logger.DebugS("map", $"Binding grid {actualID} to entity {grid.GridEntity}");
                }
            }

            OnGridCreated?.Invoke(actualID);
            return grid;
        }

        public IMapGrid GetGrid(GridId gridID)
        {
            return _grids[gridID];
        }

        public bool TryGetGrid(GridId gridId, out IMapGrid grid)
        {
            if (_grids.TryGetValue(gridId, out var gridinterface))
            {
                grid = gridinterface;
                return true;
            }
            grid = null;
            return false;
        }

        public bool GridExists(GridId gridID)
        {
            return _grids.ContainsKey(gridID);
        }

        public IEnumerable<IMapGrid> GetAllMapGrids(MapId mapId)
        {
            return _grids.Values.Where(m => m.ParentMapId == mapId);
        }

        public IMapGrid FindGridAt(MapId mapId, Vector2 worldPos)
        {
            var defaultGrid = GetDefaultGrid(mapId);
            foreach (var grid in GetAllMapGrids(mapId))
                if (grid.WorldBounds.Contains(worldPos) && grid != defaultGrid)
                    return grid;
            return defaultGrid;
        }

        public IMapGrid FindGridAt(MapCoordinates mapCoords)
        {
            return FindGridAt(mapCoords.MapId, mapCoords.Position);
        }

        public IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2 worldArea)
        {
            return GetAllMapGrids(mapId).Where(grid => grid.WorldBounds.Intersects(worldArea));
        }

        public void DeleteGrid(GridId gridID)
        {
            // nullspace grid cannot be deleted
            if(gridID == GridId.Nullspace)
                return;

            var grid = _grids[gridID];

            grid.Dispose();
            _grids.Remove(grid.Index);

            if (_defaultGrids.ContainsKey(grid.ParentMapId))
                _defaultGrids.Remove(grid.ParentMapId);
            
            OnGridRemoved?.Invoke(gridID);

            if (_netManager.IsServer)
                _gridDeletionHistory.Add((_gameTiming.CurTick, gridID));
        }
    }

    /// <summary>
    ///     Arguments for when a map is created or deleted locally ore remotely.
    /// </summary>
    public class MapEventArgs : EventArgs
    {
        /// <summary>
        ///     Map that is being modified.
        /// </summary>
        public MapId Map { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public MapEventArgs(MapId map)
        {
            Map = map;
        }
    }

    /// <summary>
    ///     Arguments for when a single tile on a grid is changed locally or remotely.
    /// </summary>
    public class TileChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     New tile that replaced the old one.
        /// </summary>
        public TileRef NewTile { get; }

        /// <summary>
        ///     Old tile that was replaced.
        /// </summary>
        public Tile OldTile { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public TileChangedEventArgs(TileRef newTile, Tile oldTile)
        {
            NewTile = newTile;
            OldTile = oldTile;
        }
    }

    /// <summary>
    ///     Arguments for when a one or more tiles on a grid is changed at once.
    /// </summary>
    public class GridChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Grid being changed.
        /// </summary>
        public IMapGrid Grid { get; }

        public IReadOnlyCollection<(MapIndices position, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridChangedEventArgs(IMapGrid grid, IReadOnlyCollection<(MapIndices position, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
