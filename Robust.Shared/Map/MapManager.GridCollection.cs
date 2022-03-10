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
public sealed class GridChangedEventArgs : EventArgs
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
public sealed class TileChangedEventArgs : EventArgs
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
    private readonly Dictionary<GridId, EntityUid> _grids = new();

    private GridId _highestGridId = GridId.Invalid;

    public virtual void ChunkRemoved(GridId gridId, MapChunk chunk) { }

    public EntityUid GetGridEuid(GridId id)
    {
        DebugTools.Assert(id != GridId.Invalid);

        //This turns into a linear search with EntityQuery without the _grids mapping
        return _grids[id];
    }

    public bool TryGetGridEuid(GridId id, [MaybeNullWhen(false)] out EntityUid euid)
    {
        DebugTools.Assert(id != GridId.Invalid);

        if (_grids.TryGetValue(id, out euid))
            return true;

        euid = default;
        return false;
    }

    public IMapGridComponent GetGridComp(GridId id)
    {
        DebugTools.Assert(id != GridId.Invalid);

        var euid = GetGridEuid(id);
        return GetGridComp(euid);
    }

    public IMapGridComponent GetGridComp(EntityUid euid)
    {
        return EntityManager.GetComponent<IMapGridComponent>(euid);
    }

    public bool TryGetGridComp(GridId id, [MaybeNullWhen(false)] out IMapGridComponent comp)
    {
        DebugTools.Assert(id != GridId.Invalid);

        var euid = GetGridEuid(id);
        if (EntityManager.TryGetComponent(euid, out comp))
            return true;

        comp = default;
        return false;
    }

    /// <inheritdoc />
    public void BindGrid(MapGridComponent gridComponent, MapGrid mapGrid)
    {
        gridComponent.Grid = mapGrid;
        gridComponent.GridIndex = mapGrid.Index;
        mapGrid.GridEntityId = gridComponent.Owner;

        _grids.Add(mapGrid.Index, mapGrid.GridEntityId);
        Logger.InfoS("map", $"Binding grid {mapGrid.Index} to entity {gridComponent.Owner}");
        OnGridCreated?.Invoke(mapGrid.ParentMapId, mapGrid.Index);
    }

    /// <inheritdoc />
    public MapGrid CreateUnboundGrid()
    {
        var actualId = GenerateGridId(null);
        var grid = new MapGrid(this, EntityManager, actualId, 16);
        Logger.InfoS("map", $"Creating unbound grid {grid.Index}");
        return grid;
    }

    public IEnumerable<IMapGrid> GetAllGrids()
    {
        return EntityManager.EntityQuery<IMapGridComponent>(true).Select(c => c.Grid);
    }

    public IMapGrid CreateGrid(MapId currentMapId, GridId? forcedGridId = null, ushort chunkSize = 16)
    {
        return CreateGrid(currentMapId, forcedGridId, chunkSize, default);
    }

    public IMapGrid GetGrid(EntityUid euid)
    {
        return GetGridComp(euid).Grid;
    }

    public IMapGrid GetGrid(GridId gridId)
    {
        DebugTools.Assert(gridId != GridId.Invalid);

        var euid = GetGridEuid(gridId);
        return GetGridComp(euid).Grid;
    }

    public bool IsGrid(EntityUid uid)
    {
        return EntityManager.HasComponent<IMapGridComponent>(uid);
    }

    public bool TryGetGrid(EntityUid euid, [MaybeNullWhen(false)] out IMapGrid grid)
    {
        if (EntityManager.TryGetComponent(euid, out IMapGridComponent comp))
        {
            grid = comp.Grid;
            return true;
        }

        grid = default;
        return false;
    }

    public bool TryGetGrid(GridId gridId, [MaybeNullWhen(false)] out IMapGrid grid)
    {
        // grid 0 compatibility
        if (gridId == GridId.Invalid)
        {
            grid = default;
            return false;
        }

        if (!TryGetGridEuid(gridId, out var euid))
        {
            grid = default;
            return false;
        }

        return TryGetGrid(euid, out grid);
    }

    public bool GridExists(GridId gridId)
    {
        // grid 0 compatibility
        return gridId != GridId.Invalid && TryGetGridEuid(gridId, out var euid) && GridExists(euid);
    }

    public bool GridExists(EntityUid euid)
    {
        return EntityManager.EntityExists(euid) && EntityManager.HasComponent<IMapGridComponent>(euid);
    }

    public IEnumerable<IMapGrid> GetAllMapGrids(MapId mapId)
    {
        return EntityManager.EntityQuery<IMapGridComponent>(true)
            .Where(c => c.Grid.ParentMapId == mapId)
            .Select(c => c.Grid);
    }

    public void FindGridsIntersectingEnumerator(MapId mapId, Box2 worldAabb, out FindGridsEnumerator enumerator, bool approx = false)
    {
        enumerator = new FindGridsEnumerator(EntityManager, GetAllGrids().Cast<MapGrid>().GetEnumerator(), mapId, worldAabb, approx);
    }

    public virtual void DeleteGrid(GridId gridId)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        // Possible the grid was already deleted / is invalid
        if (!TryGetGrid(gridId, out var iGrid))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with unknown id {gridId}.");
            return; // Silently fail on release
        }

        var grid = (MapGrid)iGrid;
        if (grid.Deleting)
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} multiple times for grid {gridId}.");
            return; // Silently fail on release
        }

        var entityId = grid.GridEntityId;
        if (!EntityManager.TryGetComponent(entityId, out MetaDataComponent metaComp))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with {gridId}, but there was no allocated entity.");
            return; // Silently fail on release
        }

        // DeleteGrid may be triggered by the entity being deleted,
        // so make sure that's not the case.
        if (metaComp.EntityLifeStage < EntityLifeStage.Terminating)
            EntityManager.DeleteEntity(entityId);
    }

    public void TrueGridDelete(MapGrid grid)
    {
        grid.Deleting = true;

        var mapId = grid.ParentMapId;
        var gridId = grid.Index;

        _grids.Remove(grid.Index);

        Logger.DebugS("map", $"Deleted grid {gridId}");

        // TODO: Remove this trash
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
        var euid = GetGridEuid(tileRef.GridIndex);
        EntityManager.EventBus.RaiseLocalEvent(euid, new TileChangedEvent(tileRef, oldTile));
    }

    protected MapGrid CreateGrid(MapId currentMapId, GridId? forcedGridId, ushort chunkSize, EntityUid forcedGridEuid)
    {
        var actualId = GenerateGridId(forcedGridId);
        var grid = new MapGrid(this, EntityManager, actualId, chunkSize);

        var gridEnt = EntityManager.CreateEntityUninitialized(null, forcedGridEuid);
        var gridComp = EntityManager.AddComponent<MapGridComponent>(gridEnt);

        gridComp.Grid = grid;
        gridComp.GridIndex = grid.Index;
        grid.GridEntityId = gridEnt;

        _grids.Add(grid.Index, grid.GridEntityId);
        Logger.DebugS("map", $"Binding new grid {grid.Index} to entity {grid.GridEntityId}");

        //TODO: This is a hack to get TransformComponent.MapId working before entity states
        //are applied. After they are applied the parent may be different, but the MapId will
        //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
        //use in transform states anytime before the state parent is properly set.
        EntityUid fallbackParentEuid = GetMapEntityIdOrThrow(currentMapId);
        EntityManager.GetComponent<TransformComponent>(gridEnt).AttachParent(fallbackParentEuid);

        EntityManager.InitializeComponents(grid.GridEntityId);
        EntityManager.StartComponents(grid.GridEntityId);
        OnGridCreated?.Invoke(currentMapId, grid.Index);
        return grid;
    }

    protected void InvokeGridChanged(object? sender, GridChangedEventArgs ev)
    {
        GridChanged?.Invoke(sender, ev);
        var args = new GridModifiedEvent(ev.Grid, ev.Modified);
        EntityManager.EventBus.RaiseLocalEvent(ev.Grid.GridEntityId, args);
    }

    private GridId GenerateGridId(GridId? forcedGridId)
    {
        var actualId = forcedGridId ?? new GridId(_highestGridId.Value + 1);

        if(actualId == GridId.Invalid)
            throw new InvalidOperationException($"Cannot allocate a grid with an Invalid ID.");

        if (GridExists(actualId))
            throw new InvalidOperationException($"A grid with ID {actualId} already exists");

        if (_highestGridId.Value < actualId.Value)
            _highestGridId = actualId;
        return actualId;
    }
}
