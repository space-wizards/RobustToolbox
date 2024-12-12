using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using Robust.Server.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects;

public sealed class LightSensitiveSystem : SharedLightSensitiveSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly LightTreeSystem _lightTreeSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private float _cooldown = 0.5f;

    private TimeSpan _targetTime = TimeSpan.Zero;


    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (_cooldown <= 0f)
            return;

        if (_gameTiming.CurTime < _targetTime)
            return;

        _targetTime = _gameTiming.CurTime + TimeSpan.FromSeconds(_cooldown);
        //var occluderQuery = GetEntityQuery<OccluderComponent>();

        var query = EntityQueryEnumerator<LightSensitiveComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            ProcessNearbyLights(uid, comp, xform);
        }
    }

    public bool TryGetLightLevel(
        EntityUid uid,
        [NotNullWhen(true)] out LightSensitiveComponent? component,
        bool forceUpdate = false
        )
    {
        var comp = EnsureComp<LightSensitiveComponent>(uid);

        // We only want to run this update if enough time has passed or it's REALLY important
        // that a component/system operate on precise or frequent updates.
        // For example: If a non player entity like a plant takes damage from being in the dark, that doesn't
        // really need updates every single tick.
        if (comp.LastUpdate + TimeSpan.FromSeconds(_cooldown) < _gameTiming.CurTime || forceUpdate)
            ProcessNearbyLights(uid, comp, Transform(uid));

        component = comp;
        return true;
    }

    private void ProcessNearbyLights(EntityUid uid, LightSensitiveComponent component, TransformComponent playerXform)
    {
        var illumination = 0f;

        var ourPosition = _transform.GetMapCoordinates(uid);
        //var bounds = _physics.GetWorldAABB(uid);
        var box = Box2.CenteredAround(ourPosition.Position, new Vector2(0.5f, 0.5f));
        var queryResult = _lightTreeSystem.QueryAabb(ourPosition.MapId, box);

        foreach (var val in queryResult)
        {
            var (lightComp, lightxform) = val;
            if (!lightComp.Enabled)
                continue;

            var lightPosition = _transform.GetMapCoordinates(lightxform).Offset(lightComp.Offset);

            if (!InRangeUnOccluded(lightPosition, ourPosition, lightComp.Radius, null))
                continue;

            playerXform.Coordinates.TryDistance(EntityManager, lightxform.Coordinates, out var dist);
            //Log.Debug($"distance from light {lightPosition}: {dist}, light energy: {lightComp.Energy}, light radius: {lightComp.Radius}");

            var calculatedLight = 0f;
            if (_proto.TryIndex(lightComp.LightMask, out var mask))
            {
                var angleToTarget = GetAngle(val.Uid, lightxform, lightComp, uid, playerXform);
                foreach (var cone in mask.Cones)
                {
                    if (Math.Abs(angleToTarget.Degrees) - cone.Direction > cone.OuterWidth)
                    {
                        //Log.Debug($"{uid} outside of cone");
                        continue;
                    }
                    else if (
                        Math.Abs(angleToTarget.Degrees) - cone.Direction > cone.InnerWidth &&
                        Math.Abs(angleToTarget.Degrees) - cone.Direction < cone.OuterWidth)
                    {
                        calculatedLight = Math.Clamp(
                            lightComp.Energy * (1 - dist / lightComp.Radius) *
                            (float)Math.Cos(angleToTarget + MathHelper.DegreesToRadians(cone.OuterWidth - cone.InnerWidth)), 0f, 1f);

                        //Log.Debug($"{uid} between inner and outer angle of cone");
                    }
                    else
                    {
                        calculatedLight = Math.Clamp(lightComp.Energy * (1 - dist / lightComp.Radius), 0f, 1f);
                        //Log.Debug($"{uid} fully within cone");
                    }
                }
            }
            else
                calculatedLight = Math.Clamp(lightComp.Energy * (1 - dist / lightComp.Radius), 0f, 1f);

            illumination = Math.Max(illumination, calculatedLight);
        }

        SetIllumination(uid, illumination, component);
        component.LastUpdate = _gameTiming.CurTime;
    }

    public override bool InRangeUnOccluded<TState>(MapCoordinates origin, MapCoordinates other, float range,
            TState state, Func<EntityUid, TState, bool> predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
    {
        if (other.MapId != origin.MapId ||
            other.MapId == MapId.Nullspace) return false;

        var dir = other.Position - origin.Position;
        var length = dir.Length();

        // If range specified also check it
        // TODO: This rounding check is here because the API is kinda eh
        if (range > 0f && length > range + 0.01f) return false;

        if (MathHelper.CloseTo(length, 0)) return true;

        if (length > MaxRaycastRange)
        {
            Log.Warning("InRangeUnOccluded check performed over extreme range. Limiting CollisionRay size.");
            length = MaxRaycastRange;
        }

        var ray = new Ray(origin.Position, dir.Normalized());
        bool Ignored(EntityUid entity) => TryComp<OccluderComponent>(entity, out var o) && !o.Enabled;

        var rayResults = _occluder
            .IntersectRayWithPredicate(origin.MapId, ray, length, state, predicate: (e, ts) => TryComp<OccluderComponent>(e, out var o) && !o.Enabled, false);

        if (rayResults.Count == 0) return true;

        if (!ignoreInsideBlocker) return false;

        foreach (var result in rayResults)
        {
            if (!TryComp(result.HitEntity, out OccluderComponent? o))
            {
                continue;
            }

            if (!o.Enabled)
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

    public override bool InRangeUnOccluded(EntityUid origin, EntityUid other, float range = 3f, Ignored? predicate = null, bool ignoreInsideBlocker = true)
    {

        var originPos = _transform.GetMapCoordinates(origin);
        var otherPos = _transform.GetMapCoordinates(other);

        return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
    }

    public override bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
    {
        // No, rider. This is better.
        // ReSharper disable once ConvertToLocalFunction
        var wrapped = (EntityUid uid, Ignored? wrapped)
            => wrapped != null && wrapped(uid);

        return InRangeUnOccluded(origin, other, range, predicate, wrapped, ignoreInsideBlocker, entMan);
    }

    public Angle GetAngle(EntityUid lightUid, TransformComponent lightXform, PointLightComponent lightComp, EntityUid targetUid, TransformComponent targetXform)
    {
        var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightXform);
        lightPos += lightRot.RotateVec(lightComp.Offset);

        var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform);

        // var lightCOM = Robust.Shared.Physics.Transform.Mul(new Transform(lightPos, lightRot),
        //     _physicsQuery.GetComponent(lightUid).LocalCenter);
        // var targetCOM = Robust.Shared.Physics.Transform.Mul(new Transform(targetPos, targetRot),
        //     _physicsQuery.GetComponent(targetUid).LocalCenter);

        var mapDiff = targetPos - lightPos;

        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        var angle = oppositeMapDiff.ToWorldAngle();
        return angle;
    }

}
