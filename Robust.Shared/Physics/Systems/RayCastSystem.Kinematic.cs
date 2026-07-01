using System;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Systems;

public sealed partial class RayCastSystem
{
    private const int DefaultKinematicMoveIterations = 4;
    private const float DefaultKinematicMoveSkin = 0f;
    private const float KinematicMoveEpsilon = float.Epsilon;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Moves a shape through the physics broadphase and clips the remainder of the translation along hit planes.
    /// </summary>
    /// <remarks>
    /// Does not depenetrate shapes that are already overlapping at <paramref name="origin"/>.
    /// </remarks>
    public KinematicMoveResult CollideAndSlide(
        MapId mapId,
        IPhysShape shape,
        Transform origin,
        Vector2 translation,
        QueryFilter filter,
        int maxIterations = DefaultKinematicMoveIterations,
        float skin = DefaultKinematicMoveSkin)
    {
        var result = new KinematicMoveResult();
        var transform = origin;
        var remaining = translation;
        skin = MathF.Max(0f, skin);

        for (var i = 0; i < maxIterations; i++)
        {
            if (remaining.LengthSquared() <= KinematicMoveEpsilon)
            {
                result.Translation += remaining;
                remaining = Vector2.Zero;
                break;
            }

            if (!TryCastShapeClosest(mapId, shape, transform, remaining, filter, out var hit))
            {
                result.Translation += remaining;
                remaining = Vector2.Zero;
                break;
            }

            var length = remaining.Length();
            var safeFraction = Math.Clamp(hit.Fraction - skin / length, 0f, 1f);
            var travel = remaining * safeFraction;

            result.Translation += travel;
            result.Hits.Add(hit);
            transform.Position += travel;

            var left = remaining * (1f - safeFraction);
            var intoPlane = Vector2.Dot(left, hit.Normal);
            remaining = intoPlane < 0f
                ? left - hit.Normal * intoPlane
                : left;
        }

        result.Remainder = remaining;
        return result;
    }

    /// <summary>
    /// Moves all hard fixtures on an entity through the physics broadphase and clips the remainder of the translation along hit planes.
    /// </summary>
    /// <remarks>
    /// Does not depenetrate fixtures that are already overlapping.
    /// </remarks>
    public KinematicMoveResult CollideAndSlide(
        EntityUid uid,
        Vector2 translation,
        Func<EntityUid, bool>? ignored = null,
        int maxIterations = DefaultKinematicMoveIterations,
        float skin = DefaultKinematicMoveSkin,
        FixturesComponent? fixtures = null,
        TransformComponent? xform = null)
    {
        if (!_fixturesQuery.Resolve(uid, ref fixtures, false) ||
            !_xformQuery.Resolve(uid, ref xform, false) ||
            xform.MapID == MapId.Nullspace)
        {
            return new KinematicMoveResult { Translation = translation };
        }

        var result = new KinematicMoveResult();
        var transform = _physics.GetPhysicsTransform(uid, xform);
        var remaining = translation;
        skin = MathF.Max(0f, skin);

        for (var i = 0; i < maxIterations; i++)
        {
            if (remaining.LengthSquared() <= KinematicMoveEpsilon)
            {
                result.Translation += remaining;
                remaining = Vector2.Zero;
                break;
            }

            if (!TryCastBodyClosest(uid, xform.MapID, fixtures, transform, remaining, ignored, out var hit))
            {
                result.Translation += remaining;
                remaining = Vector2.Zero;
                break;
            }

            var length = remaining.Length();
            var safeFraction = Math.Clamp(hit.Fraction - skin / length, 0f, 1f);
            var travel = remaining * safeFraction;

            result.Translation += travel;
            result.Hits.Add(hit);
            transform.Position += travel;

            var left = remaining * (1f - safeFraction);
            var intoPlane = Vector2.Dot(left, hit.Normal);
            remaining = intoPlane < 0f
                ? left - hit.Normal * intoPlane
                : left;
        }

        result.Remainder = remaining;
        return result;
    }

    private bool TryCastBodyClosest(
        EntityUid uid,
        MapId mapId,
        FixturesComponent fixtures,
        Transform origin,
        Vector2 translation,
        Func<EntityUid, bool>? ignored,
        out KinematicMoveHit hit)
    {
        hit = default;
        var found = false;
        Func<EntityUid, bool> isIgnored = IsIgnored;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard ||
                fixture.CollisionLayer == 0x0 ||
                fixture.CollisionMask == 0x0 ||
                !CanKinematicMoveCastShape(fixture.Shape))
            {
                continue;
            }

            var filter = new QueryFilter
            {
                LayerBits = fixture.CollisionLayer,
                MaskBits = fixture.CollisionMask,
                IsIgnored = isIgnored,
            };

            if (!TryCastShapeClosest(mapId, fixture.Shape, origin, translation, filter, out var fixtureHit))
                continue;

            if (!found || fixtureHit.Fraction < hit.Fraction)
            {
                hit = fixtureHit;
                found = true;
            }
        }

        return found;

        bool IsIgnored(EntityUid other)
        {
            return other == uid || ignored?.Invoke(other) == true;
        }
    }

    private bool TryCastShapeClosest(
        MapId mapId,
        IPhysShape shape,
        Transform origin,
        Vector2 translation,
        QueryFilter filter,
        out KinematicMoveHit hit)
    {
        hit = default;

        if (mapId == MapId.Nullspace || translation.LengthSquared() <= KinematicMoveEpsilon)
            return false;

        var startAabb = shape.ComputeAABB(origin, 0);
        var endAabb = shape.ComputeAABB(new Transform(origin.Position + translation, origin.Quaternion2D.Angle), 0);
        var aabb = startAabb.Union(endAabb);
        var state = (origin, translation, shape, filter, system: this, physics: _physics, hit, found: false);

        _broadphase.GetBroadphases(mapId,
            aabb,
            ref state,
            static (entity, ref tuple) =>
            {
                var transform = tuple.physics.GetPhysicsTransform(entity.Owner);
                var localOrigin = Physics.Transform.MulT(transform, tuple.origin);
                var localTranslation = Physics.Transform.InvTransformPoint(transform, tuple.origin.Position + tuple.translation) - localOrigin.Position;
                var rayResult = new RayResult();

                tuple.system.CastShape(
                    (entity.Owner, entity.Comp),
                    ref rayResult,
                    tuple.shape,
                    localOrigin,
                    localTranslation,
                    tuple.filter,
                    RayCastClosestCallback);

                if (!rayResult.Hit)
                    return;

                var rayHit = rayResult.Results[0];
                var point = Physics.Transform.Mul(transform, rayHit.Point);
                var normal = Quaternion2D.RotateVector(transform.Quaternion2D, rayHit.LocalNormal);
                var hit = new KinematicMoveHit(rayHit.Entity, point, normal, rayHit.Fraction);

                if (!tuple.found || hit.Fraction < tuple.hit.Fraction)
                {
                    tuple.hit = hit;
                    tuple.found = true;
                }
            });

        hit = state.hit;
        return state.found;
    }

    private static bool CanKinematicMoveCastShape(IPhysShape shape)
    {
        return shape switch
        {
            PhysShapeCircle => true,
            PolygonShape => true,
            Polygon => true,
            SlimPolygon => true,
            _ => false,
        };
    }
}

public record struct KinematicMoveResult()
{
    /// <summary>
    /// Translation that can be safely applied after clipping against blocking shapes.
    /// </summary>
    public Vector2 Translation;

    /// <summary>
    /// Remaining clipped translation after all iterations.
    /// </summary>
    public Vector2 Remainder;

    public ValueList<KinematicMoveHit> Hits = new();

    public readonly bool Hit => Hits.Count > 0;
}

public readonly record struct KinematicMoveHit(EntityUid Entity, Vector2 Point, Vector2 Normal, float Fraction);
