using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;
internal partial class MapManager
{
    // ReSharper disable once MethodOverloadWithOptionalParameter
    [Obsolete("use SharedMapSystem.CreateGridEntity(...).Comp")]
    public MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize = 16)
    {
        return CreateGridEntity(currentMapId, options: GridCreateOptions.Default with { ChunkSize = chunkSize }).Comp;
    }

    [Obsolete("use SharedMapSystem.CreateGridEntity(...).Comp")]
    public MapGridComponent CreateGrid(MapId currentMapId, in GridCreateOptions options)
    {
        return CreateGridEntity(currentMapId, options: options).Comp;
    }

    [Obsolete("use SharedMapSystem.CreateGridEntity(...).Comp")]
    public MapGridComponent CreateGrid(MapId currentMapId)
    {
        return CreateGridEntity(currentMapId, options: GridCreateOptions.Default).Comp;
    }

    [Obsolete("use SharedMapSystem.CreateGridEntity")]
    public Entity<MapGridComponent> CreateGridEntity(MapId currentMapId, GridCreateOptions? options = null)
    {
        return MapSystem.CreateGridEntity(currentMapId, options: options);
    }

    [Obsolete("use SharedMapSystem.CreateGridEntity")]
    public Entity<MapGridComponent> CreateGridEntity(EntityUid map, GridCreateOptions? options = null)
    {
        return MapSystem.CreateGridEntity(map, options: options);
    }

    [Obsolete("Use HasComponent<MapGridComponent>(uid)")]
    public bool IsGrid(EntityUid uid)
    {
        return EntityManager.HasComponent<MapGridComponent>(uid);
    }

    [Obsolete("use SharedMapSystem.GetAllMapGrids")]
    public IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId)
    {
        return MapSystem.GetAllMapGrids(mapId);
    }

    [Obsolete("use SharedMapSystem.GetAllGrids")]
    public IEnumerable<Entity<MapGridComponent>> GetAllGrids(MapId mapId)
    {
        return MapSystem.GetAllGrids(mapId);
    }

    [Obsolete("just delete the grid entity")]
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
    [Obsolete("use SharedMapSystem.SuppressOnTileChanged")]
    public bool SuppressOnTileChanged
    {
        get => MapSystem.SuppressOnTileChanged;
        set { MapSystem.SuppressOnTileChanged = value; }
    }

    /// <summary>
    ///     Raises the OnTileChanged event.
    /// </summary>
    /// <param name="tileRef">A reference to the new tile.</param>
    /// <param name="oldTile">The old tile that got replaced.</param>
    [Obsolete("use SharedMapSystem.RaiseOnTileChanged")]
    void IMapManagerInternal.RaiseOnTileChanged(Entity<MapGridComponent> entity, TileRef tileRef, Tile oldTile, Vector2i chunk)
    {
        MapSystem.RaiseOnTileChanged(entity, tileRef, oldTile, chunk);
    }
}
