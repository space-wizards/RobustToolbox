using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;
public abstract partial class OccluderSystem : ComponentTreeSystem<OccluderTreeComponent, OccluderComponent>
{
    public const float MaxRaycastRange = 100f;

    [Dependency] private FixtureSystem _fixtureSystem = default!;

    [Dependency] private EntityQuery<OccluderComponent> _occluderQuery;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery;

    private readonly List<RayCastResults> _raycastResults = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OccluderComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<OccluderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnCompInit(EntityUid uid, OccluderComponent comp, ComponentInit args)
    {
        UpdatePolygonCache(comp);
    }

    private void OnAfterAutoHandleState(EntityUid uid, OccluderComponent comp, ref AfterAutoHandleStateEvent args)
    {
        UpdatePolygonCache(comp);
        QueueTreeUpdate(uid, comp);
        OnOccluderAfterAutoHandleState(uid, comp, ref args);
    }

    protected virtual void OnOccluderAfterAutoHandleState(EntityUid uid, OccluderComponent comp, ref AfterAutoHandleStateEvent args)
    {
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
        var position = entry.Transform.LocalPosition;
        var local = GetTreeLocalBounds(entry.Component);
        return new Box2Rotated(
            local.Translated(position),
            entry.Transform.LocalRotation,
            position).CalcBoundingBox();
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<OccluderComponent> entry, Vector2 pos, Angle rot)
        => new Box2Rotated(GetTreeLocalBounds(entry.Component).Translated(pos), rot, pos).CalcBoundingBox();

    /// <summary>
    /// Local bounds used for tree queries. Expands to include any visual render hull so Clyde can find it.
    /// </summary>
    public static Box2 GetTreeLocalBounds(OccluderComponent occluder)
    {
        var bounds = occluder.LocalBounds;
        if (occluder.VisualLocalBounds == Box2.Empty)
            return bounds;

        if (!occluder.AlignVisualToEye)
            return bounds.Union(occluder.VisualLocalBounds);

        // Client may swing tall lighting with the local eye — pad isotropically for tree queries.
        return bounds.Enlarged(GetVisualExtraHeight(occluder));
    }

    /// <summary>
    /// Extra height of the visual hull beyond <see cref="OccluderComponent.LocalBounds"/>.
    /// </summary>
    public static float GetVisualExtraHeight(OccluderComponent occluder)
    {
        if (occluder.VisualSize.Y <= 0f)
            return 0f;

        return MathF.Max(0f, occluder.VisualSize.Y - occluder.LocalBounds.Height);
    }

    /// <summary>
    /// For a tall visual extending along <paramref name="tallAxis"/>, only footprint edges parallel to
    /// that axis stay coincident with neighbours. Default CW quad edge bits: 0=N, 1=E, 2=S, 3=W.
    /// </summary>
    public static byte GetTallVisualSharedEdgeMask(byte footprintMask, Direction tallAxis)
    {
        const byte northSouth = 1 << 0 | 1 << 2;
        const byte eastWest = 1 << 1 | 1 << 3;

        return tallAxis switch
        {
            Direction.North or Direction.South => (byte) (footprintMask & eastWest),
            Direction.East or Direction.West => (byte) (footprintMask & northSouth),
            _ => footprintMask,
        };
    }

    /// <summary>
    /// Extra-height strip on the <paramref name="tallAxis"/> face of <see cref="OccluderComponent.LocalBounds"/>.
    /// Separate from the footprint so N/S (or E/W) neighbours do not overlap side faces.
    /// Edge order is always N,E,S,W of the strip AABB.
    /// </summary>
    public static bool TryCopyExtraHeightStrip(
        OccluderComponent occluder,
        Span<Vector2> destination,
        Direction tallAxis,
        out int count)
    {
        count = 0;
        if (destination.Length < 4)
            return false;

        var extra = GetVisualExtraHeight(occluder);
        if (extra <= 0f)
            return false;

        var b = occluder.LocalBounds;
        // NW → NE → SE → SW
        switch (tallAxis)
        {
            case Direction.South:
                destination[0] = new Vector2(b.Left, b.Bottom);
                destination[1] = new Vector2(b.Right, b.Bottom);
                destination[2] = new Vector2(b.Right, b.Bottom - extra);
                destination[3] = new Vector2(b.Left, b.Bottom - extra);
                break;
            case Direction.East:
                destination[0] = new Vector2(b.Right, b.Top);
                destination[1] = new Vector2(b.Right + extra, b.Top);
                destination[2] = new Vector2(b.Right + extra, b.Bottom);
                destination[3] = new Vector2(b.Right, b.Bottom);
                break;
            case Direction.West:
                destination[0] = new Vector2(b.Left - extra, b.Top);
                destination[1] = new Vector2(b.Left, b.Top);
                destination[2] = new Vector2(b.Left, b.Bottom);
                destination[3] = new Vector2(b.Left - extra, b.Bottom);
                break;
            default: // North
                destination[0] = new Vector2(b.Left, b.Top + extra);
                destination[1] = new Vector2(b.Right, b.Top + extra);
                destination[2] = new Vector2(b.Right, b.Top);
                destination[3] = new Vector2(b.Left, b.Top);
                break;
        }

        count = 4;
        return true;
    }

    /// <summary>
    /// Cardinal matching a <c>snapCardinals</c> sprite's texture-up after its entity matrix
    /// (<c>worldRotation - RoundToCardinal(world+eye)</c>), in the occluder's local axes.
    /// </summary>
    public static Direction GetLocalScreenUpCardinal(Angle worldRotation, Angle eyeRotation)
    {
        // SpriteSystem SnapCardinals: entityMatrix rot = worldRotation - RoundToCardinal(world+eye).
        var cardinal = (worldRotation + eyeRotation).RoundToCardinalAngle();
        var tallWorld = (worldRotation - cardinal).RotateVec(new Vector2(0, 1));
        var localTall = (-worldRotation).RotateVec(tallWorld);
        return Angle.FromWorldVec(localTall).GetCardinalDir();
    }
    #endregion

    #region Setters
    public virtual void SetPolygon(EntityUid uid, Vector2[]? polygon, OccluderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.PolygonArray = polygon ??
        [
            new(-0.5f, 0.5f),
            new(0.5f, 0.5f),
            new(0.5f, -0.5f),
            new(-0.5f, -0.5f),
        ];
        UpdatePolygonCache(comp);
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

    public virtual void SetVisualSize(
        EntityUid uid,
        Vector2 size,
        Vector2 offset = default,
        OccluderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp.VisualSize.Equals(size) && comp.VisualOffset.Equals(offset))
            return;

        comp.VisualSize = size;
        comp.VisualOffset = offset;
        UpdatePolygonCache(comp);
        Dirty(uid, comp);
        QueueTreeUpdate(uid, comp);
    }

    public virtual void SetAlignVisualToEye(EntityUid uid, bool align, OccluderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.AlignVisualToEye == align)
            return;

        comp.AlignVisualToEye = align;
        UpdatePolygonCache(comp);
        Dirty(uid, comp);
        QueueTreeUpdate(uid, comp);
    }
    #endregion

    protected override void OnCompStartup(EntityUid uid, OccluderComponent component, ComponentStartup args)
    {
        UpdatePolygonCache(component);
        base.OnCompStartup(uid, component, args);
    }

    private static void UpdatePolygonCache(OccluderComponent occluder)
    {
        occluder.LocalBounds = CalculateLocalBounds(occluder.Polygon);
        occluder.VisualLocalBounds = Box2.Empty;

        if (occluder.VisualSize.X > 0f && occluder.VisualSize.Y > 0f)
        {
            var half = occluder.VisualSize * 0.5f;
            var o = occluder.VisualOffset;
            occluder.VisualLocalBounds = new Box2(o.X - half.X, o.Y - half.Y, o.X + half.X, o.Y + half.Y);
        }
    }

    /// <summary>
    /// True when tall lighting is configured via <see cref="OccluderComponent.VisualSize"/>.
    /// </summary>
    public static bool HasVisualSize(OccluderComponent occluder)
    {
        return occluder.VisualSize.X > 0f && occluder.VisualSize.Y > 0f;
    }

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
        if (!GetRay(origin, other, range, out var length, out var ray, out var result))
            return result;

        IntersectRay(_raycastResults, origin.MapId, ray, length);
        foreach (var rayResult in _raycastResults)
        {
            if (!_occluderQuery.TryComp(rayResult.HitEntity, out var occluder) ||
                !_xformQuery.TryComp(rayResult.HitEntity, out var xform))
            {
                return false;
            }

            if (!occluder.BlockVision)
                continue;

            // Tree AABB includes the tall visual hull for Clyde; vision only uses the gameplay polygon.
            if (!RayHitsVisionHull(occluder, xform, ray, length))
                continue;

            if (!ignore(new Entity<OccluderComponent, TransformComponent>(rayResult.HitEntity, occluder, xform), state))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if two points are within the specified range and there are no occluders between them.
    /// </summary>
    /// <param name="ignoreTouching">If true, this will ignore occluders that contain the start or end point.</param>
    public bool InRangeUnoccluded(MapCoordinates origin, MapCoordinates other, float range, bool ignoreTouching)
    {
        if (!GetRay(origin, other, range, out var length, out var ray, out var result))
            return result;

        IntersectRay(_raycastResults, origin.MapId, ray, length);
        foreach (var rayResult in _raycastResults)
        {
            if (!_occluderQuery.TryComp(rayResult.HitEntity, out var occluder) ||
                !_xformQuery.TryComp(rayResult.HitEntity, out var xform))
            {
                return false;
            }

            if (!occluder.BlockVision)
                continue;

            if (!RayHitsVisionHull(occluder, xform, ray, length))
                continue;

            if (!ignoreTouching)
                return false;

            if (ContainsPoint(occluder, xform, origin.Position) ||
                ContainsPoint(occluder, xform, other.Position))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// True when <paramref name="worldRay"/> intersects the gameplay vision hull (<see cref="OccluderComponent.LocalBounds"/>),
    /// ignoring any taller visual FOV/light hull.
    /// </summary>
    public bool RayHitsVisionHull(
        OccluderComponent occluder,
        TransformComponent xform,
        in Ray worldRay,
        float maxLength)
    {
        var (worldPos, worldRot) = XformSystem.GetWorldPositionRotation(xform);
        var invRot = -worldRot;
        var localOrigin = invRot.RotateVec(worldRay.Position - worldPos);
        var localDir = invRot.RotateVec(worldRay.Direction);
        return RayIntersectsAabb(occluder.LocalBounds, localOrigin, localDir, maxLength);
    }

    /// <summary>
    /// Slab test: ray vs axis-aligned box in the same space.
    /// </summary>
    private static bool RayIntersectsAabb(Box2 box, Vector2 origin, Vector2 direction, float maxLength)
    {
        var tMin = 0f;
        var tMax = maxLength;

        if (!ClipSlab(origin.X, direction.X, box.Left, box.Right, ref tMin, ref tMax) ||
            !ClipSlab(origin.Y, direction.Y, box.Bottom, box.Top, ref tMin, ref tMax))
            return false;

        return tMax >= tMin && tMax >= 0f && tMin <= maxLength;
    }

    private static bool ClipSlab(float origin, float dir, float min, float max, ref float tMin, ref float tMax)
    {
        const float epsilon = 1e-6f;
        if (MathF.Abs(dir) < epsilon)
            return origin >= min && origin <= max;

        var t1 = (min - origin) / dir;
        var t2 = (max - origin) / dir;
        if (t1 > t2)
            (t1, t2) = (t2, t1);

        tMin = MathF.Max(tMin, t1);
        tMax = MathF.Min(tMax, t2);
        return tMin <= tMax;
    }

    private bool GetRay(MapCoordinates origin, MapCoordinates other, float range, out float length, out Ray ray, out bool result)
    {
        ray = default;
        length = default;
        result = false;
        if (other.MapId != origin.MapId || other.MapId == MapId.Nullspace)
            return false;

        var dir = other.Position - origin.Position;
        length = dir.Length();
        if (MathHelper.CloseTo(length, 0))
        {
            result = true;
            return false;
        }

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

    public bool ContainsPoint(OccluderComponent occluder, TransformComponent xform, Vector2 point)
    {
        // Broadphase check — gameplay bounds only (not tall visual).
        var (worldPosition, worldRotation) = XformSystem.GetWorldPositionRotation(xform);
        var worldBounds = new Box2Rotated(
            occluder.LocalBounds.Translated(worldPosition),
            worldRotation,
            worldPosition).CalcBoundingBox();

        if (!worldBounds.Contains(point))
            return false;

        // Narrowphase check
        var polygon = new Polygon(occluder.PolygonArray);
        return polygon.VertexCount >= 3 &&
               _fixtureSystem.TestPoint(polygon, new Transform(worldPosition, worldRotation), point);
    }

    private static Box2 CalculateLocalBounds(ReadOnlySpan<Vector2> polygon)
    {
        var bounds = new Box2(polygon[0], polygon[0]);
        for (var i = 1; i < polygon.Length; i++)
        {
            bounds = bounds.ExtendToContain(polygon[i]);
        }

        return bounds;
    }
    #endregion
}
