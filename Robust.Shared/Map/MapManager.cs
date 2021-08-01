using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <inheritdoc cref="IMapManager"/>
    internal class MapManager : IMapManagerInternal
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

        private protected readonly HashSet<MapId> _maps = new();
        private protected readonly Dictionary<MapId, GameTick> _mapCreationTick = new();

        private protected readonly Dictionary<GridId, MapGrid> _grids = new();
        private protected readonly Dictionary<MapId, EntityUid> _mapEntities = new();

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

        public void OnComponentRemoved(MapGridComponent comp)
        {
            var gridIndex = comp.GridIndex;
            if (gridIndex != GridId.Invalid)
            {
                if (GridExists(gridIndex))
                {
                    Logger.DebugS("map",
                        $"Entity {comp.Owner.Uid} removed grid component, removing bound grid {gridIndex}");
                    DeleteGrid(gridIndex);
                }
            }
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
                {
                    Logger.InfoS("map", $"Deleting map entity {entId}");
                    entity.Delete();
                }

                if (_mapEntities.Remove(MapId.Nullspace))
                    Logger.InfoS("map", "Removing nullspace map entity.");
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
        public virtual void DeleteMap(MapId mapID)
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

            Logger.InfoS("map", $"Deleting map {mapID}");
        }

        public MapId CreateMap(MapId? mapID = null)
        {
            return CreateMap(mapID, null);
        }

        public MapId CreateMap(MapId? mapID, EntityUid? entityUid)
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
                var mapComps = _entityManager.ComponentManager.EntityQuery<IMapComponent>(true);

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
                    var newEnt = (Entity) _entityManager.CreateEntityUninitialized(null, entityUid);
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

        public IMapGrid CreateGrid(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16)
        {
            return CreateGridImpl(currentMapID, gridID, chunkSize, true, null);
        }

        public IMapGrid CreateGrid(MapId currentMapID, GridId gridID, ushort chunkSize, EntityUid euid)
        {
            return CreateGridImpl(currentMapID, gridID, chunkSize, true, euid);
        }

        private IMapGridInternal CreateGridImpl(MapId currentMapID, GridId? gridID, ushort chunkSize, bool createEntity,
            EntityUid? euid)
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

            DebugTools.Assert(actualID != GridId.Invalid);

            if (GridExists(actualID))
            {
                throw new InvalidOperationException($"A grid with ID {actualID} already exists");
            }

            if (HighestGridID.Value < actualID.Value)
            {
                HighestGridID = actualID;
            }

            var grid = new MapGrid(this, _entityManager, actualID, chunkSize, currentMapID);
            _grids.Add(actualID, grid);
            Logger.InfoS("map", $"Creating new grid {actualID}");

            if (actualID != GridId.Invalid && createEntity) // nullspace default grid is not bound to an entity
            {
                // the entity may already exist from map deserialization
                IMapGridComponent? result = null;
                foreach (var comp in _entityManager.ComponentManager.EntityQuery<IMapGridComponent>(true))
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
                    var gridEnt = (Entity) EntityManager.CreateEntityUninitialized(null, euid);

                    grid.GridEntityId = gridEnt.Uid;

                    Logger.DebugS("map", $"Binding grid {actualID} to entity {grid.GridEntityId}");

                    var gridComp = gridEnt.AddComponent<MapGridComponent>();
                    gridComp.GridIndex = grid.Index;

                    //TODO: This is a hack to get TransformComponent.MapId working before entity states
                    //are applied. After they are applied the parent may be different, but the MapId will
                    //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
                    //use in transform states anytime before the state parent is properly set.
                    gridEnt.Transform.AttachParent(GetMapEntity(currentMapID));

                    gridEnt.InitializeComponents();
                    gridEnt.StartAllComponents();
                }
            }
            else
            {
                Logger.DebugS("map", $"Skipping entity binding for gridId {actualID}");
            }

            OnGridCreated?.Invoke(currentMapID, actualID);
            return grid;
        }

        public IMapGridInternal CreateGridNoEntity(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16)
        {
            return CreateGridImpl(currentMapID, gridID, chunkSize, false, null);
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
            foreach (var (_, mapGrid) in _grids)
            {
                if (mapGrid.ParentMapId != mapId)
                    continue;

                var gridEnt = _entityManager.GetEntity(mapGrid.GridEntityId);

                if (!gridEnt.TryGetComponent(out PhysicsComponent? body)) continue;

                var transform = new Transform(gridEnt.Transform.WorldPosition, (float) gridEnt.Transform.WorldRotation);

                // We can't rely on the broadphase proxies existing as they're deferred until physics requires the update.
                foreach (var fixture in body.Fixtures)
                {
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        // TODO: Need TestPoint from Box2D
                        if (fixture.Shape.ComputeAABB(transform, i).Contains(worldPos))
                        {
                            grid = mapGrid;
                            return true;
                        }
                    }
                }
            }

            grid = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryFindGridAt(MapCoordinates mapCoordinates, [NotNullWhen(true)] out IMapGrid? grid)
        {
            return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out grid);
        }

        public IEnumerable<IMapGrid> FindGridsIntersecting(MapId mapId, Box2 worldArea)
        {
            foreach (var (_, grid) in _grids)
            {
                if (grid.ParentMapId != mapId) continue;

                var found = false;

                // TODO: Future optimisation; we can probably do a very fast check with no fixtures
                // by just knowing the chunk bounds of the grid and then checking that first
                // this will mainly be helpful once we get multiple grids.
                var gridEnt = _entityManager.GetEntity(grid.GridEntityId);
                var body = gridEnt.GetComponent<PhysicsComponent>();
                var transform = new Transform(gridEnt.Transform.WorldPosition, (float) gridEnt.Transform.WorldRotation);

                // We can't rely on the broadphase proxies existing as they're deferred until physics requires the update.
                foreach (var fixture in body.Fixtures)
                {
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        // TODO: Need to use CollisionManager to test detailed overlap
                        if (fixture.Shape.ComputeAABB(transform, i).Intersects(worldArea))
                        {
                            yield return grid;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }
            }
        }

        public IEnumerable<GridId> FindGridIdsIntersecting(MapId mapId, Box2 worldArea, bool includeInvalid = false)
        {
            var broadphase = EntitySystem.Get<SharedBroadphaseSystem>();

            foreach (var broady in broadphase.GetBroadphases(mapId, worldArea))
            {
                if (!broady.Owner.TryGetComponent(out MapGridComponent? mapGridComponent)) continue;

                yield return mapGridComponent.GridIndex;

                // TODO: Optimise this. Need to avoid returning invalid unless absolutely necessary but still this check
                // be hella expensive. Also doesn't account for rotation so.
                if (broady.Owner.GetComponent<PhysicsComponent>().GetWorldAABB().Encloses(worldArea))
                {
                    yield break;
                }
            }

            yield return GridId.Invalid;
        }

        public virtual void DeleteGrid(GridId gridID)
        {
#if DEBUG
            DebugTools.Assert(_dbgGuardRunning);
#endif

            if (gridID == GridId.Invalid)
                return;

            var grid = _grids[gridID];
            var mapId = grid.ParentMapId;

            if (_entityManager.TryGetEntity(grid.GridEntityId, out var gridEnt) &&
                gridEnt.LifeStage <= EntityLifeStage.Initialized)
                gridEnt.Delete();

            grid.Dispose();
            _grids.Remove(grid.Index);

            OnGridRemoved?.Invoke(mapId, gridID);
        }

        public MapId NextMapId()
        {
            return HighestMapID = new MapId(HighestMapID.Value + 1);
        }

        public GridId NextGridId()
        {
            return HighestGridID = new GridId(HighestGridID.Value + 1);
        }

        protected void InvokeGridChanged(object? sender, GridChangedEventArgs ev)
        {
            GridChanged?.Invoke(sender, ev);
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
