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
    private GridId _highestGridId = GridId.Invalid;
    public virtual void ChunkRemoved(EntityUid gridId, MapChunk chunk) { }

    public MapGridComponent GetGridComp(EntityUid euid)
    {
        return EntityManager.GetComponent<MapGridComponent>(euid);
    }

    public IEnumerable<MapGridComponent> GetAllGrids()
    {
        return EntityManager.EntityQuery<MapGridComponent>();
    }

    public MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options)
    {
        return CreateGrid(currentMapId, null, options.ChunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize)
    {
        return CreateGrid(currentMapId, null, chunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId)
    {
        return CreateGrid(currentMapId, GridCreateOptions.Default);
    }

    public MapGridComponent GetGrid(EntityUid gridId)
    {
        DebugTools.Assert(gridId.IsValid());

        return GetGridComp(gridId);
    }

    public bool IsGrid(EntityUid uid)
    {
        return EntityManager.HasComponent<MapGridComponent>(uid);
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

    public bool GridExists([NotNullWhen(true)] EntityUid? euid)
    {
        return EntityManager.HasComponent<MapGridComponent>(euid);
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

    /// <inheritdoc />
    public event EventHandler<TileChangedEventArgs>? TileChanged;

    /// <summary>
    ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
    ///     so that you don't spam an event for each of the million station tiles.
    /// </summary>
    /// <inheritdoc />
    public event EventHandler<GridChangedEventArgs>? GridChanged;

    /// <inheritdoc />
    public bool SuppressOnTileChanged { get; set; }

    public virtual void OnComponentRemoved(MapGridComponent comp)
    {
        var entityId = comp.GridEntityId;
        if (!EntityManager.TryGetComponent(entityId, out MetaDataComponent? metaComp))
        {
            DebugTools.Assert($"Calling {nameof(OnComponentRemoved)} with {comp.Owner}, but there was no allocated entity.");
            return; // Silently fail on release
        }

        // DeleteGrid may be triggered by the entity being deleted,
        // so make sure that's not the case.
        if (metaComp.EntityLifeStage < EntityLifeStage.Terminating)
            EntityManager.DeleteEntity(entityId);
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
        var euid = tileRef.GridUid;
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
            grid = preInit.Comp;
        }

        Logger.DebugS("map", $"Binding new grid {grid.GridIndex} to entity {grid.GridEntityId}");

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
        
        if (_highestGridId.Value < actualId.Value)
            _highestGridId = actualId;

        if(forcedGridId is not null) // this function basically just passes forced gridIds through.
            Logger.DebugS("map", $"Allocating new GridId {actualId}.");

        return actualId;
    }
}
