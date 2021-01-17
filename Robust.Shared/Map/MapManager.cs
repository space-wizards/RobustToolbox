using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <inheritdoc cref="IMapManager"/>
    internal partial class MapManager : IMapManagerInternal
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public IGameTiming GameTiming => _gameTiming;

        public IEntityManager EntityManager => _entityManager;

        /// <inheritdoc />
        public MapId DefaultMap => MapId.Nullspace;

        /// <inheritdoc />
        public event EventHandler<TileChangedEventArgs>? TileChanged;

        public event GridEventHandler? OnGridCreated;

        public event GridEventHandler? OnGridRemoved;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        /// <inheritdoc />
        public event EventHandler<GridChangedEventArgs>? GridChanged;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs>? MapCreated;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs>? MapDestroyed;

        /// <inheritdoc />
        public bool SuppressOnTileChanged { get; set; }

        private MapId HighestMapID = MapId.Nullspace;
        private GridId HighestGridID = GridId.Invalid;

        private readonly HashSet<MapId> _maps = new();
        private readonly Dictionary<MapId, GameTick> _mapCreationTick = new();

        private readonly Dictionary<GridId, MapGrid> _grids = new();
        private readonly Dictionary<MapId, EntityUid> _mapEntities = new();

        private readonly List<(GameTick tick, GridId gridId)> _gridDeletionHistory = new();
        private readonly List<(GameTick tick, MapId mapId)> _mapDeletionHistory = new();

#if DEBUG
        private bool _dbgGuardInit = false;
        private bool _dbgGuardRunning = false;
#endif

        /// <inheritdoc />
        public void Initialize()
        {
#if DEBUG
            DebugTools.Assert(!_dbgGuardInit);
            DebugTools.Assert(!_dbgGuardRunning);
            _dbgGuardInit = true;
#endif
        }

        /// <inheritdoc />
        public void Startup()
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardInit);
            _dbgGuardRunning = true;
#endif

            Logger.DebugS("map", "Starting...");

            if (!_maps.Contains(MapId.Nullspace))
            {
                CreateMap(MapId.Nullspace);
            }
            else if (_mapEntities.TryGetValue(MapId.Nullspace, out var mapEntId))
            {
                var mapEnt = _entityManager.GetEntity(mapEntId);

                foreach (var childTransform in mapEnt.Transform.Children.ToArray())
                {
                    childTransform.Owner.Delete();
                }
            }

            DebugTools.Assert(_grids.Count == 0);
            DebugTools.Assert(!GridExists(GridId.Invalid));
        }

        /// <inheritdoc />
        public void Shutdown()
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardInit);
#endif
            Logger.DebugS("map", "Stopping...");

            foreach (var map in _maps.ToArray())
            {
                if (map != MapId.Nullspace)
                {
                    DeleteMap(map);
                }
            }

            if (_mapEntities.TryGetValue(MapId.Nullspace, out var entId))
            {
                if (_entityManager.TryGetEntity(entId, out var entity))
                    entity.Delete();

                _mapEntities.Remove(MapId.Nullspace);
            }

#if DEBUG
            DebugTools.Assert(_grids.Count == 0);
            DebugTools.Assert(!GridExists(GridId.Invalid));
            _dbgGuardRunning = false;
#endif
        }

        /// <inheritdoc />
        public void Restart()
        {
            Logger.DebugS("map", "Restarting...");

            Shutdown();
            Startup();
        }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        public void RaiseOnTileChanged(TileRef tileRef, Tile oldTile)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            if (SuppressOnTileChanged)
                return;

            TileChanged?.Invoke(this, new TileChangedEventArgs(tileRef, oldTile));
        }

        /// <inheritdoc />
        public void DeleteMap(MapId mapID)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            if (!_maps.Contains(mapID))
            {
                throw new InvalidOperationException($"Attempted to delete nonexistant map '{mapID}'");
            }

            // grids are cached because Delete modifies collection
            foreach (var grid in GetAllMapGrids(mapID).ToList())
            {
                DeleteGrid(grid.Index);
            }

            if (mapID != MapId.Nullspace)
            {
                MapDestroyed?.Invoke(this, new MapEventArgs(mapID));
                _maps.Remove(mapID);
                _mapCreationTick.Remove(mapID);
            }

            if (_mapEntities.TryGetValue(mapID, out var ent))
            {
                if (_entityManager.TryGetEntity(ent, out var mapEnt))
                    mapEnt.Delete();

                _mapEntities.Remove(mapID);
            }

            if (_netManager.IsClient)
                return;

            _mapDeletionHistory.Add((_gameTiming.CurTick, mapID));
        }

        public MapId CreateMap(MapId? mapID = null)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

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

            if (actualID != MapId.Nullspace) // nullspace isn't bound to an entity
            {
                var mapComps = _entityManager.ComponentManager.EntityQuery<IMapComponent>();

                IMapComponent? result = null;
                foreach (var mapComp in mapComps)
                {
                    if (mapComp.WorldMap != actualID)
                        continue;

                    result = mapComp;
                    break;
                }

                if (result != null)
                {
                    _mapEntities.Add(actualID, result.Owner.Uid);
                    Logger.DebugS("map", $"Rebinding map {actualID} to entity {result.Owner.Uid}");
                }
                else
                {
                    var newEnt = (Entity) _entityManager.CreateEntityUninitialized(null, EntityCoordinates.Invalid);
                    _mapEntities.Add(actualID, newEnt.Uid);

                    var mapComp = newEnt.AddComponent<MapComponent>();
                    mapComp.WorldMap = actualID;
                    newEnt.InitializeComponents();
                    newEnt.StartAllComponents();
                    Logger.DebugS("map", $"Binding map {actualID} to entity {newEnt.Uid}");
                }
            }

            MapCreated?.Invoke(this, new MapEventArgs(actualID));

            return actualID;
        }

        /// <inheritdoc />
        public bool MapExists(MapId mapID)
        {
            return _maps.Contains(mapID);
        }

        public IEntity CreateNewMapEntity(MapId mapId)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            var newEntity = (Entity) _entityManager.CreateEntityUninitialized(null);
            SetMapEntity(mapId, newEntity);

            newEntity.InitializeComponents();
            newEntity.StartAllComponents();

            return newEntity;
        }

        /// <inheritdoc />
        public void SetMapEntity(MapId mapId, EntityUid newMapEntityId)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            var newMapEntity = _entityManager.GetEntity(newMapEntityId);
            SetMapEntity(mapId, newMapEntity);
        }

        /// <inheritdoc />
        public void SetMapEntity(MapId mapId, IEntity newMapEntity)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            if (!_maps.Contains(mapId))
                throw new InvalidOperationException($"Map {mapId} does not exist.");

            foreach (var kvEntity in _mapEntities)
            {
                if (kvEntity.Value == newMapEntity.Uid)
                {
                    throw new InvalidOperationException(
                        $"Entity {newMapEntity} is already the root node of map {kvEntity.Key}.");
                }
            }

            // remove existing graph
            if (_mapEntities.TryGetValue(mapId, out var oldEntId))
            {
                //Note: This prevents setting a subgraph as the root, since the subgraph will be deleted
                var oldMapEnt = _entityManager.GetEntity(oldEntId);
                _entityManager.DeleteEntity(oldMapEnt);
            }
            else
            {
                _mapEntities.Add(mapId, EntityUid.Invalid);
            }

            // re-use or add map component
            if (!newMapEntity.TryGetComponent(out MapComponent? mapComp))
            {
                mapComp = newMapEntity.AddComponent<MapComponent>();
            }
            else
            {
                if (mapComp.WorldMap != mapId)
                    Logger.WarningS("map",
                        $"Setting map {mapId} root to entity {newMapEntity}, but entity thinks it is root node of map {mapComp.WorldMap}.");
            }

            Logger.DebugS("map", $"Setting map {mapId} entity to {newMapEntity.Uid}");

            // set as new map entity
            mapComp.WorldMap = mapId;
            _mapEntities[mapId] = newMapEntity.Uid;
        }

        public EntityUid GetMapEntityId(MapId mapId)
        {
            if (_mapEntities.TryGetValue(mapId, out var entId))
                return entId;

            return EntityUid.Invalid;
        }

        public IEntity GetMapEntity(MapId mapId)
        {
            if (!_mapEntities.ContainsKey(mapId))
                throw new InvalidOperationException($"Map {mapId} does not have a set map entity.");

            return _entityManager.GetEntity(_mapEntities[mapId]);
        }

        public bool HasMapEntity(MapId mapId)
        {
            return _mapEntities.ContainsKey(mapId);
        }

        public IEnumerable<MapId> GetAllMapIds()
        {
            return _maps;
        }

        public IEnumerable<IMapGrid> GetAllGrids()
        {
            return _grids.Values;
        }

        public IMapGrid CreateGrid(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16, float snapSize = 1)
        {
            return CreateGridImpl(currentMapID, gridID, chunkSize, snapSize, true);
        }

        private IMapGridInternal CreateGridImpl(MapId currentMapID, GridId? gridID, ushort chunkSize, float snapSize,
            bool createEntity)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

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
                throw new InvalidOperationException($"A grid with ID {actualID} already exists");
            }

            if (HighestGridID.Value < actualID.Value)
            {
                HighestGridID = actualID;
            }

            var grid = new MapGrid(this, _entityManager, actualID, chunkSize, snapSize, currentMapID);
            _grids.Add(actualID, grid);
            Logger.DebugS("map", $"Creating new grid {actualID}");

            if (actualID != GridId.Invalid && createEntity) // nullspace default grid is not bound to an entity
            {
                // the entity may already exist from map deserialization
                IMapGridComponent? result = null;
                foreach (var comp in _entityManager.ComponentManager.EntityQuery<IMapGridComponent>())
                {
                    if (comp.GridIndex != actualID)
                        continue;

                    result = comp;
                    break;
                }

                if (result != null)
                {
                    grid.GridEntityId = result.Owner.Uid;
                    Logger.DebugS("map", $"Rebinding grid {actualID} to entity {grid.GridEntityId}");
                }
                else
                {
                    var newEnt =
                        (Entity) _entityManager.CreateEntityUninitialized(null,
                            new MapCoordinates(Vector2.Zero, currentMapID));
                    grid.GridEntityId = newEnt.Uid;

                    Logger.DebugS("map", $"Binding grid {actualID} to entity {grid.GridEntityId}");

                    var gridComp = newEnt.AddComponent<MapGridComponent>();
                    gridComp.GridIndex = grid.Index;

                    var collideComp = newEnt.AddComponent<PhysicsComponent>();
                    collideComp.CanCollide = true;
                    collideComp.AddFixture(new Fixture(collideComp, new PhysShapeGrid(grid)));

                    newEnt.Transform.AttachParent(_entityManager.GetEntity(_mapEntities[currentMapID]));

                    newEnt.InitializeComponents();
                    newEnt.StartAllComponents();
                }
            }

            OnGridCreated?.Invoke(actualID);
            return grid;
        }

        public IMapGridInternal CreateGridNoEntity(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16,
            float snapSize = 1)
        {
            return CreateGridImpl(currentMapID, gridID, chunkSize, snapSize, false);
        }

        public IMapGrid GetGrid(GridId gridID)
        {
            return _grids[gridID];
        }

        public bool TryGetGrid(GridId gridId, [NotNullWhen(true)] out IMapGrid? grid)
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

        /// <inheritdoc />
        public bool TryFindGridAt(MapId mapId, Vector2 worldPos, [NotNullWhen(true)] out IMapGrid? grid)
        {
            foreach (var mapGrid in _grids.Values)
            {
                if(mapGrid.ParentMapId != mapId)
                    continue;

                if(!mapGrid.WorldBounds.Contains(worldPos))
                    continue;

                grid = mapGrid;
                return true;
            }

            grid = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out IMapGrid? grid)
        {
            return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out grid);
        }

        public IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2 worldArea)
        {
            return _grids.Values.Where(g => g.ParentMapId == mapId && g.WorldBounds.Intersects(worldArea));
        }

        public IEnumerable<GridId> FindGridIdsIntersecting(MapId mapId, Box2 worldArea, bool includeInvalid = false)
        {
            foreach (var (_, grid) in _grids)
            {
                if (grid.ParentMapId != mapId) continue;

                var gridBounds = grid.WorldBounds;
                // If the worldArea is wholly contained within a grid then no need to get invalid
                if (gridBounds.Encloses(worldArea))
                {
                    yield return grid.Index;
                    yield break;
                }

                if (gridBounds.Intersects(worldArea))
                {
                    yield return grid.Index;
                }
            }

            if (includeInvalid)
                yield return GridId.Invalid;
        }

        public void DeleteGrid(GridId gridID)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            if (gridID == GridId.Invalid)
                return;

            var grid = _grids[gridID];

            if (_entityManager.TryGetEntity(grid.GridEntityId, out var gridEnt))
                gridEnt.Delete();

            grid.Dispose();
            _grids.Remove(grid.Index);

            OnGridRemoved?.Invoke(gridID);

            if (_netManager.IsServer)
                _gridDeletionHistory.Add((_gameTiming.CurTick, gridID));
        }

        public MapId NextMapId()
        {
            return HighestMapID = new MapId(HighestMapID.Value + 1);
        }

        public GridId NextGridId()
        {
            return HighestGridID = new GridId(HighestGridID.Value + 1);
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

        public IReadOnlyCollection<(Vector2i position, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridChangedEventArgs(IMapGrid grid, IReadOnlyCollection<(Vector2i position, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
