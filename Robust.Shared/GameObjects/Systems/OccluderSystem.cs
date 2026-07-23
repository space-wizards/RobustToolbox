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

    [Dependency] private EntityQuery<OccluderComponent> _occluderQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

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
        return new Box2Rotated(
            entry.Component.LocalBounds.Translated(position),
            entry.Transform.LocalRotation,
            position).CalcBoundingBox();
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<OccluderComponent> entry, Vector2 pos, Angle rot)
        => new Box2Rotated(entry.Component.LocalBounds.Translated(pos), rot, pos).CalcBoundingBox();
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
    #endregion

    protected override void OnCompStartup(EntityUid uid, OccluderComponent component, ComponentStartup args)
    {
        UpdatePolygonCache(component);
        base.OnCompStartup(uid, component, args);
    }

    private static void UpdatePolygonCache(OccluderComponent occluder)
    {
        occluder.LocalBounds = CalculateLocalBounds(occluder.Polygon);
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
            if (!ignoreTouching)
                return false;

            if (!_occluderQuery.TryComp(rayResult.HitEntity, out var occluder) ||
                !_xformQuery.TryComp(rayResult.HitEntity, out var xform))
            {
                return false;
            }

            if (ContainsPoint(occluder, xform, origin.Position) ||
                ContainsPoint(occluder, xform, other.Position))
            {
                continue;
            }

            return false;
        }

        return true;
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
        // Broadphase check
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
