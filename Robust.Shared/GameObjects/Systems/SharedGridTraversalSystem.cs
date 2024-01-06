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

    private EntityUid _recursionGuard;

    /// <summary>
    /// Enables or disables changing grid / map uid upon moving.
    /// </summary>
    public bool Enabled = true;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformStartupEvent>(OnStartup);
        _transform.OnGlobalMoveEvent += OnMove;
    }

    private void OnStartup(ref TransformStartupEvent ev)
    {
        CheckTraverse(ev.Entity.Owner, ev.Entity.Comp);
    }

    public override void Shutdown()
    {
        _transform.OnGlobalMoveEvent -= OnMove;
    }

    private void OnMove(ref MoveEvent moveEv)
    {
        CheckTraverse(moveEv.Sender, moveEv.Component);
    }


    internal void CheckTraverse(EntityUid uid, TransformComponent xform)
    {
        if (!Enabled || _timing.ApplyingState)
            return;

        // Grid-traversal can result in a stack overflow. This is probably because of rounding errors when checking
        // grid intersections using the map vs grid coordinates.
        if (uid == _recursionGuard)
            return;

        // Don't run logic if:
        // - Current parent is not a grid / map
        // - Is anchored
        // - Is a grid/map
        // - Is in nullspace

        if ((xform.GridUid != xform.ParentUid && xform.MapUid != xform.ParentUid)
            || xform.Anchored
            || uid == xform.GridUid
            || uid == xform.MapUid
            || xform.MapUid is not {} map)
        {
            return;
        }

        if (_recursionGuard != EntityUid.Invalid)
        {
            Log.Error($"Grid traversal attempted to handle movement of {ToPrettyString(uid)} while moving {ToPrettyString(_recursionGuard)}");
            return;
        }

        _recursionGuard = uid;
        try
        {
            CheckTraversal(uid, xform, map);
        }
        finally
        {
            _recursionGuard = default;
        }
    }


    public void CheckTraversal(EntityUid entity, TransformComponent xform, EntityUid map)
    {
        DebugTools.Assert(!HasComp<MapGridComponent>(entity));
        DebugTools.Assert(!HasComp<MapComponent>(entity));

        var mapPos = xform.ParentUid == xform.MapUid
            ? xform.LocalPosition
            : Transform(xform.ParentUid).LocalMatrix.Transform(xform.LocalPosition);

        // Change parent if necessary
        if (_mapManager.TryFindGridAt(map, mapPos, out var gridUid, out _))
        {
            // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
            if (gridUid != xform.GridUid)
                _transform.SetParent(entity, xform, gridUid);
            return;
        }

        // Attach them to map / they are on an invalid grid
        if (xform.GridUid != null)
            _transform.SetParent(entity, xform, map);
    }
}

