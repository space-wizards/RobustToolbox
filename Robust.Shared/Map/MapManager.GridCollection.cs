using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <summary>
///     Arguments for when a one or more tiles on a grid is changed at once.
/// </summary>
public class GridChangedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public GridChangedEventArgs(IMapGrid grid, IReadOnlyCollection<(Vector2i position, Tile tile)> modified)
    {
        Grid = grid;
        Modified = modified;
    }

    /// <summary>
    ///     Grid being changed.
    /// </summary>
    public IMapGrid Grid { get; }

    public IReadOnlyCollection<(Vector2i position, Tile tile)> Modified { get; }
}

/// <summary>
///     Arguments for when a single tile on a grid is changed locally or remotely.
/// </summary>
public class TileChangedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public TileChangedEventArgs(TileRef newTile, Tile oldTile)
    {
        NewTile = newTile;
        OldTile = oldTile;
    }

    /// <summary>
    ///     New tile that replaced the old one.
    /// </summary>
    public TileRef NewTile { get; }

    /// <summary>
    ///     Old tile that was replaced.
    /// </summary>
    public Tile OldTile { get; }
}

internal partial class MapManager
{
    private protected readonly Dictionary<GridId, MapGrid> Grids = new();

    private GridId _highestGridId = GridId.Invalid;

    public virtual void ChunkRemoved(MapChunk chunk) { }

    public IEnumerable<IMapGrid> GetAllGrids()
    {
        return Grids.Values;
    }

    public IMapGrid CreateGrid(MapId currentMapId, GridId? gridId = null, ushort chunkSize = 16)
    {
        return CreateGridImpl(currentMapId, gridId, chunkSize, true, default);
    }

    public IMapGridInternal CreateGridNoEntity(MapId currentMapId, GridId? gridId = null, ushort chunkSize = 16)
    {
        return CreateGridImpl(currentMapId, gridId, chunkSize, false, default);
    }

    public IMapGrid GetGrid(GridId gridId)
    {
        return Grids[gridId];
    }

    public bool IsGrid(EntityUid uid)
    {
        return Grids.Any(x => x.Value.GridEntityId == uid);
    }

    public bool TryGetGrid(GridId gridId, [NotNullWhen(true)] out IMapGrid? grid)
    {
        if (Grids.TryGetValue(gridId, out var mapGrid))
        {
            grid = mapGrid;
            return true;
        }

        grid = null;
        return false;
    }

    public bool GridExists(GridId gridId)
    {
        return Grids.ContainsKey(gridId);
    }

    public IEnumerable<IMapGrid> GetAllMapGrids(MapId mapId)
    {
        return Grids.Values.Where(m => m.ParentMapId == mapId);
    }

    public void FindGridsIntersectingEnumerator(MapId mapId, Box2 worldAabb, out FindGridsEnumerator enumerator, bool approx = false)
    {
        enumerator = new FindGridsEnumerator(EntityManager, Grids.GetEnumerator(), mapId, worldAabb, approx);
    }

    public virtual void DeleteGrid(GridId gridId)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif
        // Possible the grid was already deleted / is invalid
        if (!Grids.TryGetValue(gridId, out var grid) || grid.Deleting)
            return;

        grid.Deleting = true;

        var mapId = grid.ParentMapId;

        var entityId = grid.GridEntityId;

        if (EntityManager.EntityExists(entityId))
        {
            // DeleteGrid may be triggered by the entity being deleted,
            // so make sure that's not the case.
            if (EntityManager.GetComponent<MetaDataComponent>(entityId).EntityLifeStage <= EntityLifeStage.MapInitialized)
                EntityManager.DeleteEntity(entityId);
        }

        grid.Dispose();
        Grids.Remove(grid.Index);

        Logger.DebugS("map", $"Deleted grid {gridId}");
        OnGridRemoved?.Invoke(mapId, gridId);
    }

    public GridId NextGridId()
    {
        return _highestGridId = new GridId(_highestGridId.Value + 1);
    }

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
    public bool SuppressOnTileChanged { get; set; }

    public void OnComponentRemoved(MapGridComponent comp)
    {
        var gridIndex = comp.GridIndex;
        if (gridIndex == GridId.Invalid)
            return;

        if (!GridExists(gridIndex))
            return;

        Logger.DebugS("map", $"Entity {comp.Owner} removed grid component, removing bound grid {gridIndex}");
        DeleteGrid(gridIndex);
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

    protected IMapGrid CreateGrid(MapId currentMapId, GridId gridId, ushort chunkSize, EntityUid euid)
    {
        return CreateGridImpl(currentMapId, gridId, chunkSize, true, euid);
    }

    protected void InvokeGridChanged(object? sender, GridChangedEventArgs ev)
    {
        GridChanged?.Invoke(sender, ev);
    }

    private IMapGridInternal CreateGridImpl(MapId currentMapId, GridId? gridId, ushort chunkSize, bool createEntity,
        EntityUid euid)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        var actualId = gridId ?? new GridId(_highestGridId.Value + 1);

        DebugTools.Assert(actualId != GridId.Invalid);

        if (GridExists(actualId))
            throw new InvalidOperationException($"A grid with ID {actualId} already exists");

        if (_highestGridId.Value < actualId.Value)
            _highestGridId = actualId;

        var grid = new MapGrid(this, EntityManager, actualId, chunkSize, currentMapId);
        Grids.Add(actualId, grid);
        Logger.InfoS("map", $"Creating new grid {actualId}");

        if (actualId != GridId.Invalid && createEntity) // nullspace default grid is not bound to an entity
        {
            // the entity may already exist from map deserialization
            IMapGridComponent? result = null;
            foreach (var comp in EntityManager.EntityQuery<MapGridComponent>(true))
            {
                if (comp.GridIndex != actualId)
                    continue;

                result = comp;
                break;
            }

            if (result != null)
            {
                grid.GridEntityId = result.Owner;
                Logger.DebugS("map", $"Rebinding grid {actualId} to entity {grid.GridEntityId}");
            }
            else
            {
                var gridEnt = EntityManager.CreateEntityUninitialized(null, euid);

                grid.GridEntityId = gridEnt;

                Logger.DebugS("map", $"Binding grid {actualId} to entity {grid.GridEntityId}");

                var gridComp = EntityManager.AddComponent<MapGridComponent>(gridEnt);
                gridComp.GridIndex = grid.Index;

                //TODO: This is a hack to get TransformComponent.MapId working before entity states
                //are applied. After they are applied the parent may be different, but the MapId will
                //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
                //use in transform states anytime before the state parent is properly set.
                EntityManager.GetComponent<TransformComponent>(gridEnt).AttachParent(GetMapEntityIdOrThrow(currentMapId));

                EntityManager.InitializeComponents(gridEnt);
                EntityManager.StartComponents(gridEnt);
            }
        }
        else
            Logger.DebugS("map", $"Skipping entity binding for gridId {actualId}");

        OnGridCreated?.Invoke(currentMapId, actualId);
        return grid;
    }
}
