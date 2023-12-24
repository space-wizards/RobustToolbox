using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Handles moving entities between grids as they move around.
/// </summary>
internal sealed class SharedGridTraversalSystem : EntitySystem
{
    [Dependency] private readonly IMapManagerInternal _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Enables or disables changing grid / map uid upon moving.
    /// </summary>
    public bool Enabled = true;

    public override void Initialize()
    {
        base.Initialize();
        _transform.OnGlobalMoveEvent += OnMove;
    }

    public override void Shutdown()
    {
        _transform.OnGlobalMoveEvent -= OnMove;
    }

    internal void CheckTraverse(EntityUid uid, TransformComponent xform)
    {
        if (!Enabled)
            return;

        var moveEv = new MoveEvent(uid, xform.Coordinates, xform.Coordinates, xform.LocalRotation, xform.LocalRotation, xform, false);

        OnMove(ref moveEv);
    }

    private void OnMove(ref MoveEvent moveEv)
    {
        if (!Enabled || _timing.ApplyingState)
            return;

        var entity = moveEv.Sender;
        var xform = moveEv.Component;

        // Don't run logic if:
        // - Current parent is not a grid / map
        // - Is anchored
        // - Is a grid/map
        // - Is in nullspace

        if ((xform.GridUid != xform.ParentUid && xform.MapUid != xform.ParentUid)
            || xform.Anchored
            || entity == xform.GridUid
            || entity == xform.MapUid
            || xform.MapUid is not {} map)
        {
            return;
        }

        DebugTools.Assert(!HasComp<MapGridComponent>(entity));
        DebugTools.Assert(!HasComp<MapComponent>(entity));

        var mapPos = xform.ParentUid == xform.MapUid
            ? xform.LocalPosition
            : Transform(xform.ParentUid).LocalMatrix.Transform(xform.LocalPosition);

        // Change parent if necessary
        if (_mapManager.TryFindGridAt(moveEv.Component.MapID, mapPos, out var gridUid, out _))
        {
            // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
            if (gridUid != xform.GridUid)
                _transform.SetParent(entity, xform, gridUid);
            return;
        }

        // Attach them to map / they are on an invalid grid
        if (xform.GridUid != null)
            _transform.SetParent(entity, xform, xform.MapUid.Value);
    }
}

