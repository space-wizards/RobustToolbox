using System;
using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;
public abstract class OccluderSystem : ComponentTreeSystem<OccluderTreeComponent, OccluderComponent>
{
    public const float MaxRaycastRange = 100f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OccluderComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<OccluderComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, OccluderComponent comp, ref ComponentGetState args)
    {
        args.State = new OccluderComponent.OccluderComponentState(comp.Enabled, comp.BoundingBox);
    }
    private void OnHandleState(EntityUid uid, OccluderComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not OccluderComponent.OccluderComponentState state)
            return;

        SetEnabled(uid, state.Enabled, comp);
        SetBoundingBox(uid, state.BoundingBox, comp);
    }

    #region Component Tree Overrides
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => true;

    // this system relies on the assumption that all occluders are parented directly to a grid or map.
    // if this ever changes, this will make server move events very expensive.
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<OccluderComponent> entry)
    {
        DebugTools.Assert(entry.Transform.ParentUid == entry.Component.TreeUid);
        return entry.Component.BoundingBox.Translated(entry.Transform.LocalPosition);
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<OccluderComponent> entry, Vector2 pos, Angle rot)
        => ExtractAabb(in entry);
    #endregion

    #region Setters
    public void SetBoundingBox(EntityUid uid, Box2 box, OccluderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.BoundingBox = box;
        Dirty(uid, comp);

        if (comp.TreeUid != null)
            QueueTreeUpdate(uid, comp);
    }

    public virtual void SetEnabled(EntityUid uid, bool enabled, OccluderComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref comp, false) || enabled == comp.Enabled)
            return;

        comp.Enabled = enabled;
        Dirty(uid, comp, meta);
        QueueTreeUpdate(uid, comp);
    }
    #endregion

    #region InRangeUnoccluded

    /// <summary>
    /// Returns true if two points are within the specified range and there are no occluders between them that aren't
    /// ignored by the predicate.
    /// </summary>
    public bool InRangeUnoccluded<TState>(
        MapCoordinates origin,
        MapCoordinates other,
        float range,
        TState state,
        Func<Entity<OccluderComponent, TransformComponent>, TState, bool> ignore)
    {
        if (!GetUnoccludedRay(origin, other, range, out var length, out var ray))
            return false;

        if (MathHelper.CloseTo(length, 0))
            return true;

        var result = IntersectRay(origin.MapId, ray, length, state, ignore);
        return result == null;
    }

    /// <summary>
    /// Returns true if two points are within the specified range and there are no occluders between them.
    /// </summary>
    /// <param name="ignoreTouching">If true, this will use <see cref="IsTouchingEndpoint"/> as a predicate to ignore \
    /// occluders that are touching the start or end point.</param>
    public bool InRangeUnoccluded(MapCoordinates origin, MapCoordinates other, float range, bool ignoreTouching)
    {
        if (!GetUnoccludedRay(origin, other, range, out var length, out var ray))
            return false;

        if (MathHelper.CloseTo(length, 0))
            return true;

        if (!ignoreTouching)
            return IntersectRay(origin.MapId, ray, length) == null;

        var state = (XformSystem, origin.Position, other.Position);
        return IntersectRay(origin.MapId, ray, length, state, IsTouchingEndpoint) == null;
    }

    private bool GetUnoccludedRay(MapCoordinates origin, MapCoordinates other, float range, out float length, out Ray ray)
    {
        ray = default;
        length = default;
        if (other.MapId != origin.MapId || other.MapId == MapId.Nullspace)
            return false;

        var dir = other.Position - origin.Position;
        length = dir.Length();
        var normalized = dir / length;

        if (range > 0f && length > range + 0.01f)
            return false;

        if (length > MaxRaycastRange)
        {
            Log.Warning($"{nameof(InRangeUnoccluded)} check performed over extreme range. Limiting range.");
            length = MaxRaycastRange;
        }

        ray = new Ray(origin.Position, normalized);
        return true;
    }

    /// <summary>
    /// Simple predicate for use with <see cref="InRangeUnoccluded"/> that will ignore any occluders that intersect the
    /// start and end points.
    /// </summary>
    public static bool IsTouchingEndpoint(Entity<OccluderComponent, TransformComponent> ent, (SharedTransformSystem Sys, Vector2 Start, Vector2 End) state)
    {
        var occluderBox = ent.Comp1.BoundingBox;
        occluderBox = occluderBox.Translated(state.Sys.GetWorldPosition(ent.Comp2));
        return occluderBox.Contains(state.Start) || occluderBox.Contains(state.End);
    }

    #endregion
}
