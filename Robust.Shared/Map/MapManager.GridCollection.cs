using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
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
    public MapGridComponent GetGridComp(EntityUid euid)
    {
        return EntityManager.GetComponent<MapGridComponent>(euid);
    }

    public IEnumerable<MapGridComponent> GetAllGrids()
    {
        var compQuery = EntityManager.AllEntityQueryEnumerator<MapGridComponent>();

        while (compQuery.MoveNext(out var comp))
        {
            yield return comp;
        }
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize = 16)
    {
        return CreateGrid(currentMapId, chunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options)
    {
        return CreateGrid(currentMapId, options.ChunkSize, default);
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
            .Where(c => xformQuery.GetComponent(c.Owner).MapID == mapId)
            .Select(c => c);
    }

    public virtual void DeleteGrid(EntityUid euid)
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardRunning);
#endif

        // Possible the grid was already deleted / is invalid
        if (!TryGetGrid(euid, out var iGrid))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with unknown uid {euid}.");
            return; // Silently fail on release
        }

        if (!EntityManager.TryGetComponent(euid, out MetaDataComponent? metaComp))
        {
            DebugTools.Assert($"Calling {nameof(DeleteGrid)} with {euid}, but there was no allocated entity.");
            return; // Silently fail on release
        }

        // DeleteGrid may be triggered by the entity being deleted,
        // so make sure that's not the case.
        if (metaComp.EntityLifeStage < EntityLifeStage.Terminating)
            EntityManager.DeleteEntity(euid);
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

    protected MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize, EntityUid forcedGridEuid)
    {
        var gridEnt = EntityManager.CreateEntityUninitialized(null, forcedGridEuid);

        var grid = EntityManager.AddComponent<MapGridComponent>(gridEnt);
        grid.ChunkSize = chunkSize;

        Logger.DebugS("map", $"Binding new grid {gridEnt}");

        //TODO: This is a hack to get TransformComponent.MapId working before entity states
        //are applied. After they are applied the parent may be different, but the MapId will
        //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
        //use in transform states anytime before the state parent is properly set.
        var fallbackParentEuid = GetMapEntityIdOrThrow(currentMapId);
        EntityManager.GetComponent<TransformComponent>(gridEnt).AttachParent(fallbackParentEuid);

        EntityManager.InitializeComponents(gridEnt);
        EntityManager.StartComponents(gridEnt);
        return grid;
    }

    protected internal static void InvokeGridChanged(MapManager mapManager, MapGridComponent mapGrid, IReadOnlyCollection<(Vector2i position, Tile tile)> changedTiles)
    {
        mapManager.GridChanged?.Invoke(mapManager, new GridChangedEventArgs(mapGrid, changedTiles));
        mapManager.EntityManager.EventBus.RaiseLocalEvent(mapGrid.GridEntityId, new GridModifiedEvent(mapGrid, changedTiles), true);
    }
}
