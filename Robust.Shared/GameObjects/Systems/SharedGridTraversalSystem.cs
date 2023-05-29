using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Handles moving entities between grids as they move around.
/// </summary>
internal sealed class SharedGridTraversalSystem : EntitySystem
{
    [Dependency] private readonly IMapManagerInternal _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public Stack<MoveEvent> QueuedEvents = new();
    private HashSet<EntityUid> _handledThisTick = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoveEvent>(OnMove);
    }

    internal void CheckTraverse(EntityUid uid, TransformComponent xform)
    {
        QueuedEvents.Push(new MoveEvent(uid, xform.Coordinates, xform.Coordinates, xform.LocalRotation, xform.LocalRotation, xform, false));
    }

    private void OnMove(ref MoveEvent ev)
    {
        // If move event arose from state handling, don't bother to run grid traversal logic.
        if (ev.FromStateHandling)
            return;

        if (ev.Component.MapID == MapId.Nullspace)
            return;

        QueuedEvents.Push(ev);
    }

    public void ProcessMovement()
    {
        var maps = GetEntityQuery<MapComponent>();
        var grids = GetEntityQuery<MapGridComponent>();
        var xforms = GetEntityQuery<TransformComponent>();
        var metas = GetEntityQuery<MetaDataComponent>();

        while (QueuedEvents.TryPop(out var moveEvent))
        {
            if (!_handledThisTick.Add(moveEvent.Sender)) continue;

            HandleMove(ref moveEvent, xforms, maps, grids, metas);
        }

        _handledThisTick.Clear();
    }

    private void HandleMove(
        ref MoveEvent moveEvent,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<MapComponent> maps,
        EntityQuery<MapGridComponent> grids,
        EntityQuery<MetaDataComponent> metas)
    {
        var entity = moveEvent.Sender;

        if (!metas.TryGetComponent(entity, out var meta) ||
            meta.EntityDeleted ||
            (meta.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer ||
            maps.HasComponent(entity) ||
            grids.HasComponent(entity) ||
            !xforms.TryGetComponent(entity, out var xform) ||
            // If the entity is anchored then we know for sure it's on the grid and not traversing
            xform.Anchored)
        {
            return;
        }

        // DebugTools.Assert(!float.IsNaN(moveEvent.NewPosition.X) && !float.IsNaN(moveEvent.NewPosition.Y));

        // We only do grid-traversal parent changes if the entity is currently parented to a map or a grid.
        var parentIsMap = xform.GridUid == null && maps.HasComponent(xform.ParentUid);
        if (!parentIsMap && !grids.HasComponent(xform.ParentUid))
            return;
        var mapPos = moveEvent.NewPosition.ToMapPos(EntityManager, _transform);

        // Change parent if necessary
        if (_mapManager.TryFindGridAt(xform.MapID, mapPos, out var gridUid, out _))
        {
            // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
            if (gridUid != xform.GridUid)
            {
                _transform.SetParent(entity, xform, gridUid, xforms);
                var ev = new ChangedGridEvent(entity, xform.GridUid, gridUid);
                RaiseLocalEvent(entity, ref ev);
            }
        }
        else
        {
            var oldGridId = xform.GridUid;

            // Attach them to map / they are on an invalid grid
            if (oldGridId != null)
            {
                _transform.SetParent(entity, xform, _mapManager.GetMapEntityIdOrThrow(xform.MapID));
                var ev = new ChangedGridEvent(entity, oldGridId, null);
                RaiseLocalEvent(entity, ref ev);
            }
        }
    }
}

[ByRefEvent]
public readonly record struct ChangedGridEvent(EntityUid Entity, EntityUid? OldGrid, EntityUid? NewGrid)
{
    public readonly EntityUid Entity = Entity;
    public readonly EntityUid? OldGrid = OldGrid;
    public readonly EntityUid? NewGrid = NewGrid;
}
