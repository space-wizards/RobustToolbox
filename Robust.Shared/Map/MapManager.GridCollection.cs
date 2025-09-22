using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

// All the obsolete warnings about GridId are probably useless here.
#pragma warning disable CS0618

namespace Robust.Shared.Map;
internal partial class MapManager
{
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize = 16)
    {
        return CreateGrid(GetMapEntityIdOrThrow(currentMapId), chunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options)
    {
        return CreateGrid(GetMapEntityIdOrThrow(currentMapId), options.ChunkSize, default);
    }

    public MapGridComponent CreateGrid(MapId currentMapId)
    {
        return CreateGrid(currentMapId, GridCreateOptions.Default);
    }

    public Entity<MapGridComponent> CreateGridEntity(MapId currentMapId, GridCreateOptions? options = null)
    {
        return CreateGridEntity(GetMapEntityIdOrThrow(currentMapId), options);
    }

    public Entity<MapGridComponent> CreateGridEntity(EntityUid map, GridCreateOptions? options = null)
    {
        options ??= GridCreateOptions.Default;
        return CreateGrid(map, options.Value.ChunkSize, default);
    }

    [Obsolete("Use HasComponent<MapGridComponent>(uid)")]
    public bool IsGrid(EntityUid uid)
    {
        return EntityManager.HasComponent<MapGridComponent>(uid);
    }

    public IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId)
    {
        var query = EntityManager.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var grid, out var xform))
        {
            if (xform.MapID == mapId)
                yield return grid;
        }
    }

    public IEnumerable<Entity<MapGridComponent>> GetAllGrids(MapId mapId)
    {
        var query = EntityManager.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var grid, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            yield return (uid, grid);
        }
    }

    public virtual void DeleteGrid(EntityUid euid)
    {
        // Possible the grid was already deleted / is invalid
        if (!EntityManager.TryGetComponent<MapGridComponent>(euid, out var iGrid))
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
    public bool SuppressOnTileChanged { get; set; }

    /// <summary>
    ///     Raises the OnTileChanged event.
    /// </summary>
    /// <param name="tileRef">A reference to the new tile.</param>
    /// <param name="oldTile">The old tile that got replaced.</param>
    void IMapManagerInternal.RaiseOnTileChanged(Entity<MapGridComponent> entity, TileRef tileRef, Tile oldTile, Vector2i chunk)
    {
        if (SuppressOnTileChanged)
            return;

        var ev = new TileChangedEvent(entity, tileRef, oldTile, chunk);
        EntityManager.EventBus.RaiseLocalEvent(entity.Owner, ref ev, true);
    }

    protected Entity<MapGridComponent> CreateGrid(EntityUid map, ushort chunkSize, EntityUid forcedGridEuid)
    {
        var gridEnt = EntityManager.CreateEntityUninitialized(null, forcedGridEuid);

        var grid = EntityManager.AddComponent<MapGridComponent>(gridEnt);
        grid.ChunkSize = chunkSize;

        _sawmill.Debug($"Binding new grid {gridEnt}");

        //TODO: This is a hack to get TransformComponent.MapId working before entity states
        //are applied. After they are applied the parent may be different, but the MapId will
        //be the same. This causes TransformComponent.ParentUid of a grid to be unsafe to
        //use in transform states anytime before the state parent is properly set.
        EntityManager.GetComponent<TransformComponent>(gridEnt).AttachParent(map);

        var meta = EntityManager.GetComponent<MetaDataComponent>(gridEnt);
        EntityManager.System<MetaDataSystem>().SetEntityName(gridEnt, $"grid", meta);
        EntityManager.InitializeComponents(gridEnt, meta);
        EntityManager.StartComponents(gridEnt);
        // Note that this does not actually map-initialize the grid entity, even if the map its being spawn on has already been initialized.
        // I don't know whether that is intentional or not.

        return (gridEnt, grid);
    }
}
