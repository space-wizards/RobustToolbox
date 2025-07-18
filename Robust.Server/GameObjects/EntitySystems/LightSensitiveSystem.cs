using Robust.Server.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using TerraFX.Interop.Windows;

namespace Robust.Server.GameObjects;

public sealed class LightSensitiveSystem : SharedLightSensitiveSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly LightTreeSystem _lightTreeSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private const float DefaultCooldown = 1f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<LightSensitiveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            TryGetLightLevel(uid, out var light_level);
        }
    }

    /// <summary>
    ///     Returns the illumination level of an entity. If the entity doesn't have a LightSensitiveComponent, one will be added to store the light level.
    ///     If the entity hasn't had its light level calculated, or it hasn't been updated in the last _cooldown seconds, it will be re-calculated.
    ///     Subsequent calls to this method within a short period will return the previously calculated light level, unless the forceUpdate parameter is true.
    /// </summary>
    /// <remarks>
    ///     If you're designing a system that depends on the light level of an entity, you should create a const variable that the system will
    ///     use for the cooldown. I anticipate as time goes on, more systems will use this same function for potentially reasons and all have different cooldowns.
    ///     I really hope I don't have to deal with the fallout of this design choice.
    /// <remarks>
    /// <param name="uid">Entity UID to check.</param>
    /// <param name="lightLevel">A float value to be treated as a percentage.</param>
    /// <param name="cooldown">A float value to that a light level dependent system should set for how frequent of a recalculation in light level it should need.
    /// To be treated as seconds.</param>
    /// <param name="clamped">If true, Clamp the light level that will be returned to be between 0 and 1 for 0% to 100%.
    /// If false, the return value can go beyond 100% if the nearest lights have a high enough energy value</param>
    /// <param name="forceUpdate">If true, disregard any cooldowns in place and force an update in calculated light value.</param>
    /// <returns>The illumination level of the entity as a float. Treat this as a percentage.</returns>
    public bool TryGetLightLevel(EntityUid uid, [NotNullWhen(true)] out float? lightLevel, float cooldown = DefaultCooldown, bool clamped = true, bool forceUpdate = false)
    {
        // To gauge something's light level, we need to assign it a corresponding LightSensitiveComponent if it doesn't already have one
        var comp = EnsureComp<LightSensitiveComponent>(uid);

        // We only want to run this update if enough time has passed or it's REALLY important
        // that a component/system operate on precise or frequent updates.
        // For example: If a non player entity like a plant takes damage from being in the dark, that doesn't
        // really need updates every single tick.
        if (forceUpdate || comp.NextUpdate < _gameTiming.CurTime)
        {
            ProcessNearbyLights(uid, comp, Transform(uid));
            comp.NextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(cooldown);
        }

        lightLevel = clamped ? Math.Clamp(comp.LightLevel, 0f, 1f) : comp.LightLevel;
        return true;
    }

    /// <summary>
    ///     Gets all PointLightComponents near an entity by querying the light tree with the entity's position and bounding box.
    ///     Then checks for occlusion between each light and entity by raycasting against the occluderTree, and if the entity is in range of its radius.
    ///     Lights that are in range and unoccluded then have their light level calculated by multiplying their energy by a modified attenuation formula.
    ///     Only the highest light level is kept because I don't want to mess with adding or multiplying light values together.
    /// </summary>
    /// <remarks>
    ///     This method is probably going to be very performance inefficient, so try not to use it too often. We store recent light level calculations
    ///     in the LightSensitiveComponent, so it's not necessary to calculate them every single tick.
    /// <remarks>
    /// <param name="uid">Entity UID to check.</param>
    /// <param name="component">The LightSensitiveComponent of the entity</param>
    /// <param name="entityXform">The TransformComponent of the entity</param>
    /// <returns>If the component existed in the entity.</returns>
    private void ProcessNearbyLights(EntityUid uid, LightSensitiveComponent component, TransformComponent entityXform)
    {
        var illumination = 0f;

        //var ourPosition = _transform.GetMapCoordinates(uid);
        var ourBounds = GetWorldAABB(uid, out var ourPos, out var ourRot);
        var ourMapPos = new MapCoordinates(ourPos, entityXform.MapID);
        var queryResult = _lightTreeSystem.QueryAabb(ourMapPos.MapId, ourBounds);

        foreach (var val in queryResult)
        {
            var (lightComp, lightXform) = val;

            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightXform);
            lightPos += lightRot.RotateVec(lightComp.Offset);

            var lightPosition = new MapCoordinates(lightPos, lightXform.MapID);

            if (!InRangeUnOccluded(lightPosition, ourMapPos, lightComp.Radius, null))
                continue;

            // entityXform.Coordinates.TryDistance(EntityManager, lightXform.Coordinates, out var dist);
            var dist = ourMapPos.Position - lightPosition.Position;
            //var deltaLength = delta.Length();

            var denom = dist.Length() / lightComp.Radius;
            /// A slight modification of standard inverse square falloff that incorperates the radius of the light to make it more forgiving
            var attenuation = 1 - (denom * denom);

            var calculatedLight = 0f;
            if (_proto.TryIndex(lightComp.LightMask, out var mask))
            {
                var angleToTarget = GetAngle(val.Uid, lightXform, lightComp, uid, entityXform);

                foreach (var cone in mask.LightCones)
                {
                    var coneLight = 0f;
                    var angleAttenuation = (float)Math.Min((float)Math.Max(cone.OuterWidth - angleToTarget, 0f), cone.InnerWidth) / cone.OuterWidth;
                    var absAngle = Math.Abs(angleToTarget.Degrees);

                    // Target is outside the cone's outer width angle, so ignore
                    if (absAngle - Math.Abs(cone.Direction) > cone.OuterWidth)
                    {
                        continue;
                    }
                    // Target is outside the inner cone, but inside the outer cone, so reduce the light level
                    else if (
                        absAngle - cone.Direction > cone.InnerWidth &&
                        absAngle - cone.Direction < cone.OuterWidth
                        )
                    {
                        coneLight = lightComp.Energy * attenuation * attenuation * angleAttenuation;
                    }
                    // Target is inside the inner cone, so the light level will use standard falloff.
                    else
                    {
                        coneLight = lightComp.Energy * attenuation * attenuation;
                    }
                    calculatedLight = Math.Max(calculatedLight, coneLight);
                }
            }
            // No mask, use normal falloff
            else
            {
                calculatedLight = lightComp.Energy * attenuation * attenuation;
            }

            // We only care about the strongest light level being applied to the entity. 
            // I really don't want to deal with multiplicative or additive illumination
            illumination = Math.Max(illumination, calculatedLight);
        }

        SetIllumination(uid, illumination, component);
        
    }

    public Box2 GetWorldAABB(EntityUid uid, out Vector2 worldPos, out Angle worldRot, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        worldPos = default;
        worldRot = default;

        if (!Resolve(uid, ref manager, ref body, ref xform))
            return new Box2();

        (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);

        var transform = new Transform(worldPos, (float)worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in manager.Fixtures.Values)
        {
            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        return bounds;
    }
}
