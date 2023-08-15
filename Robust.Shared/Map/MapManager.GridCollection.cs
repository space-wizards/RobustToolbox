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
internal partial class MapManager
{
    [Obsolete("Use GetComponent<MapGridComponent>(uid)")]
    public MapGridComponent GetGridComp(EntityUid euid)
    {
        return EntityManager.GetComponent<MapGridComponent>(euid);
    }

    [Obsolete("Use EntityQuery instead.")]
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

    [Obsolete("Use GetComponent<MapGridComponent>(uid)")]
    public MapGridComponent GetGrid(EntityUid gridId)
    {
        DebugTools.Assert(gridId.IsValid());

        return GetGridComp(gridId);
    }

    [Obsolete("Use HasComponent<MapGridComponent>(uid)")]
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

    [Obsolete("Use HasComponent<MapGridComponent>(uid)")]
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

        var euid = tileRef.GridUid;
        var ev = new TileChangedEvent(euid, tileRef, oldTile);
        EntityManager.EventBus.RaiseLocalEvent(euid, ref ev, true);
    }

    protected MapGridComponent CreateGrid(MapId currentMapId, ushort chunkSize, EntityUid forcedGridEuid)
    {
        var gridEnt = EntityManager.CreateEntityUninitialized(null, forcedGridEuid);

        var grid = EntityManager.AddComponent<MapGridComponent>(gridEnt);
        grid.ChunkSize = chunkSize;

        _sawmill.Debug($"Binding new grid {gridEnt}");

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
}
