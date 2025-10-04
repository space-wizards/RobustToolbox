using System;
using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;


namespace Robust.Shared.GameObjects;

public abstract class OccluderSystem : ComponentTreeSystem<OccluderTreeComponent, OccluderComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public delegate bool Ignored(EntityUid entity);

    protected const float MaxRaycastRange = 100;

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

    /// <summary>
    /// Checks if an entity is both in range and has an unobstructed line of sight of another entity. 
    /// Obstruction is caused by the raycast hitting an <see cref="OccluderComponent"/>
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="origin">Where the raycast will begin from when checking for obstructions</param>
    /// <param name="other">Target the raycast is pointed at</param>
    /// <param name="range">The range the raycast will project through the game world to check for obstructions by occluders</param>
    /// <param name="state">A reference to the state of the query</param>
    /// <param name="predicate">Conditional callback that will be used during the query to determine if an occluder should be ignored</param>
    /// <param name="ignoreInsideBlocker">Ignore occluder if the target is inside the occluder</param>
    /// <param name="entMan">The entity manager used for the check, by default will use the current entity manager</param>
    /// <returns>True if the other entity is both in range and has an unobstructed line of sight. Otherwise False</returns>
    public virtual bool InRangeUnOccluded<TState>(MapCoordinates origin, MapCoordinates other, float range,
            TState state, Func<EntityUid, TState, bool> predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
    {
        if (other.MapId != origin.MapId ||
            other.MapId == MapId.Nullspace) return false;

        var dir = other.Position - origin.Position;
        var length = dir.Length();

        if (range > 0f && length > range + 0.01f) return false;

        if (MathHelper.CloseTo(length, 0)) return true;

        if (length > MaxRaycastRange)
        {
            Log.Warning("InRangeUnOccluded check performed over extreme range. Limiting CollisionRay size.");
            length = MaxRaycastRange;
        }

        var ray = new Ray(origin.Position, dir.Normalized());
        bool Ignored(EntityUid entity, TState ts) => TryComp<OccluderComponent>(entity, out var o) && !o.Enabled;

        var rayResults = IntersectRayWithPredicate(origin.MapId, ray, length, state, predicate: Ignored, false);

        if (rayResults.Count == 0) return true;

        if (!ignoreInsideBlocker) return false;

        foreach (var result in rayResults)
        {
            if (!TryComp(result.HitEntity, out OccluderComponent? o))
            {
                continue;
            }

            var bBox = o.BoundingBox;
            bBox = bBox.Translated(_transform.GetWorldPosition(result.HitEntity));

            if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public virtual bool InRangeUnOccluded(EntityUid origin, EntityUid other, float range = 3f, Ignored? predicate = null, bool ignoreInsideBlocker = true)
    {

        var originPos = _transform.GetMapCoordinates(origin);
        var otherPos = _transform.GetMapCoordinates(other);

        return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
    }

    public virtual bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
    {
        // No, rider. This is better.
        // ReSharper disable once ConvertToLocalFunction
        var wrapped = (EntityUid uid, Ignored? wrapped)
            => wrapped != null && wrapped(uid);

        return InRangeUnOccluded(origin, other, range, predicate, wrapped, ignoreInsideBlocker, entMan);
    }
}
