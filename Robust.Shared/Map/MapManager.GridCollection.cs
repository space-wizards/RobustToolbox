using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

// All the obsolete warnings about GridId are probably useless here.
#pragma warning disable CS0618

namespace Robust.Shared.Map;

/// <summary>
///     Arguments for when a one or more tiles on a grid is changed at once.
/// </summary>
public sealed class GridChangedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public GridChangedEventArgs(MapGridComponent grid, IReadOnlyCollection<(Vector2i position, Tile tile)> modified)
    {
        Grid = grid;
        Modified = modified;
    }

    /// <summary>
    ///     Grid being changed.
    /// </summary>
    public MapGridComponent Grid { get; }

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

    public bool TryGetGridEuid(GridId id, [NotNullWhen(true)] out EntityUid? euid)
    {
        DebugTools.Assert(id != GridId.Invalid);

        if (_grids.TryGetValue(id, out var result))
        {
            euid = result;
            return true;
        }

        euid = null;
        return false;
    }

    public MapGridComponent GetGridComp(GridId id)
    {
        DebugTools.Assert(id != GridId.Invalid);

        var euid = GetGridEuid(id);
        return GetGridComp(euid);
    }

    public MapGridComponent GetGridComp(EntityUid euid)
    {
        return EntityManager.GetComponent<MapGridComponent>(euid);
    }

    public bool TryGetGridComp(GridId id, [MaybeNullWhen(false)] out MapGridComponent comp)
    {
        DebugTools.Assert(id != GridId.Invalid);

        var euid = GetGridEuid(id);
        if (EntityManager.TryGetComponent(euid, out comp))
            return true;

        comp = default;
        return false;
    }

    /// <inheritdoc />
    public void OnGridAllocated(MapGridComponent gridComponent, MapGridComponent mapGrid)
    {
        _grids.Add(mapGrid.Index, mapGrid.GridEntityId);
        Logger.InfoS("map", $"Binding grid {mapGrid.Index} to entity {gridComponent.Owner}");
        OnGridCreated?.Invoke(mapGrid.ParentMapId, mapGrid.Index);
    }

    public GridEnumerator GetAllGridsEnumerator()
    {
        var query = EntityManager.GetEntityQuery<MapGridComponent>();
        return new GridEnumerator(_grids.GetEnumerator(), query);
    }

    public IEnumerable<MapGridComponent> GetAllGrids()
    {
        var compQuery = EntityManager.GetEntityQuery<MapGridComponent>();

        foreach (var (_, uid) in _grids)
        {
            yield return compQuery.GetComponent(uid);
        }
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public MapGridComponent CreateGrid(MapId currentMapId, GridId? forcedGridId = null, ushort chunkSize = 16)
    {
        return CreateGrid(currentMapId, forcedGridId, chunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options)
    {
        return CreateGrid(currentMapId, null, options.ChunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId)
    {
        return CreateGrid(currentMapId, GridCreateOptions.Default);
    }

    public MapGridComponent GetGrid(GridId gridId)
    {
        DebugTools.Assert(gridId != GridId.Invalid);

        var euid = GetGridEuid(gridId);
        return GetGridComp(euid);
    }

    public MapGridComponent GetGrid(EntityUid gridId)
    {
        DebugTools.Assert(gridId.IsValid());

        return GetGridComp(gridId);
    }

    public bool IsGrid(EntityUid uid)
    {
        return EntityManager.HasComponent<IMapGridComponent>(uid);
    }

    public bool TryGetGrid([NotNullWhen(true)] EntityUid? euid, [MaybeNullWhen(false)] out MapGridComponent grid)
    {
        if (EntityManager.TryGetComponent(euid, out MapGridComponent? comp))
        {
            grid = comp;
            return true;
        }

        grid = default;
        return false;
    }

    [Obsolete("Use EntityUids")]
    public bool TryGetGrid(GridId gridId, [MaybeNullWhen(false)] out MapGridComponent grid)
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

    [Obsolete("Use EntityUids")]
    public bool GridExists(GridId gridId)
    {
        // grid 0 compatibility
        return gridId != GridId.Invalid && TryGetGridEuid(gridId, out var euid) && GridExists(euid);
    }

    public bool GridExists([NotNullWhen(true)] EntityUid? euid)
    {
        return EntityManager.HasComponent<IMapGridComponent>(euid);
    }

    public IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId)
    {
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();

        return EntityManager.EntityQuery<MapGridComponent>(true)
            .Where(c => xformQuery.GetComponent(c.GridEntityId).MapID == mapId);
    }

    public void FindGridsIntersectingEnumerator(MapId mapId, Box2 worldAabb, out FindGridsEnumerator enumerator, bool approx = false)
    {
        enumerator = new FindGridsEnumerator(EntityManager, GetAllGrids().GetEnumerator(), mapId, worldAabb, approx);
    }

    [Obsolete("Delete the grid's entity instead")]
    public virtual void DeleteGrid(GridId gridId)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        // Possible the grid was already deleted / is invalid
        if (!TryGetGrid(gridId, out var gridComp))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with unknown id {gridId}.");
            return; // Silently fail on release
        }

        var entityId = gridComp.GridEntityId;
        if (!EntityManager.TryGetComponent(entityId, out MetaDataComponent? metaComp))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with {gridId}, but there was no allocated entity.");
            return; // Silently fail on release
        }

        // DeleteGrid may be triggered by the entity being deleted,
        // so make sure that's not the case.
        if (metaComp.EntityLifeStage < EntityLifeStage.Terminating)
            EntityManager.DeleteEntity(entityId);
    }

    public void TrueGridDelete(MapGridComponent grid)
    {
        var mapId = grid.ParentMapId;
        var gridId = grid.Index;

        _grids.Remove(grid.Index);

        Logger.DebugS("map", $"Deleted grid {gridId}");

        // TODO: Remove this trash
        OnGridRemoved?.Invoke(mapId, gridId);
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
        EntityManager.EventBus.RaiseLocalEvent(euid, new TileChangedEvent(euid, tileRef, oldTile), true);
    }

    protected MapGridComponent CreateGrid(MapId currentMapId, GridId? forcedGridId, ushort chunkSize, EntityUid forcedGridEuid)
    {
        var gridEnt = EntityManager.CreateEntityUninitialized(null, forcedGridEuid);

        //TODO: Also known as Component.OnAdd ;)
        MapGridComponent grid;
        using (var preInit = EntityManager.AddComponentUninitialized<MapGridComponent>(gridEnt))
        {
            var actualId = GenerateGridId(forcedGridId);
            preInit.Comp.GridIndex = actualId; // Required because of MapGrid needing it in ctor
            preInit.Comp.AllocMapGrid(chunkSize, 1);
            grid = (MapGridComponent) preInit.Comp;
        }

        Logger.DebugS("map", $"Binding new grid {grid.Index} to entity {grid.GridEntityId}");

        //TODO: This is a hack to get TransformComponent.MapId working before entity states
        //are applied. After they are applied the parent may be different, but the MapId will
        //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
        //use in transform states anytime before the state parent is properly set.
        var fallbackParentEuid = GetMapEntityIdOrThrow(currentMapId);
        EntityManager.GetComponent<TransformComponent>(gridEnt).AttachParent(fallbackParentEuid);

        EntityManager.InitializeComponents(grid.GridEntityId);
        EntityManager.StartComponents(grid.GridEntityId);
        return grid;
    }

    protected internal static void InvokeGridChanged(MapManager mapManager, MapGridComponent mapGrid,
        IReadOnlyCollection<(Vector2i position, Tile tile)> changedTiles)
    {
        mapManager.GridChanged?.Invoke(mapManager, new GridChangedEventArgs(mapGrid, changedTiles));
        mapManager.EntityManager.EventBus.RaiseLocalEvent(mapGrid.GridEntityId, new GridModifiedEvent(mapGrid, changedTiles), true);
    }

    public GridId GenerateGridId(GridId? forcedGridId)
    {
        var actualId = forcedGridId ?? new GridId(_highestGridId.Value + 1);

        if(actualId == GridId.Invalid)
            throw new InvalidOperationException($"Cannot allocate a grid with an Invalid ID.");

        if (GridExists(actualId))
            throw new InvalidOperationException($"A grid with ID {actualId} already exists");

        if (_highestGridId.Value < actualId.Value)
            _highestGridId = actualId;

        if(forcedGridId is not null) // this function basically just passes forced gridIds through.
            Logger.DebugS("map", $"Allocating new GridId {actualId}.");

        return actualId;
    }
}
