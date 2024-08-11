using System;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects;

public sealed class ClientLightingSystem : EntitySystem
{
    [Dependency] private readonly LightTreeSystem _lightTree = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Clyde.LightingPassEvent>(OnLightingPass);
    }

    private void MaxLightsChanged(int value)
    {
        _maxLights = value;
        _lightsToRenderList = new (PointLightComponent, Vector2, float , Angle)[value];
        DebugTools.Assert(_maxLights >= _maxShadowcastingLights);
    }

    private void OnLightingPass(Clyde.LightingPassEvent ev)
    {
        var pass = new Clyde.LightingPass();

        // Use worldbounds for this one as we only care if the light intersects our actual bounds
        var state = (this, count: 0, shadowCastingCount: 0, _xformQuery, ev.WorldAabb);

        foreach (var (uid, comp) in _lightTree.GetIntersectingTrees(ev.MapId, ev.WorldAabb))
        {
            var bounds = _xformSystem.GetInvWorldMatrix(uid).TransformBox(ev.WorldBounds);
            comp.Tree.QueryAabb(ref state, LightQuery, bounds);
        }

        if (state.shadowCastingCount > _maxShadowcastingLights)
        {
            // There are too many lights casting shadows to fit in the scene.
            // This check must occur before occluder expansion, or else bad things happen.

            // First, partition the array based on whether the lights are shadow casting or not
            // (non shadow casting lights should be the first partition, shadow casting lights the second)
            Array.Sort(_lightsToRenderList, 0, state.count, _lightCap);

            // Next, sort just the shadow casting lights by distance.
            Array.Sort(_lightsToRenderList, state.count - state.shadowCastingCount, state.shadowCastingCount, _shadowCap);

            // Then effectively delete the furthest lights, by setting the end of the array to exclude N
            // number of shadow casting lights (where N is the number above the max number per scene.)
            state.count -= state.shadowCastingCount - _maxShadowcastingLights;
        }

        // When culling occluders later, we can't just remove any occluders outside the worldBounds.
        // As they could still affect the shadows of (large) light sources.
        // We expand the world bounds so that it encompasses the center of every light source.
        // This should make it so no culled occluder can make a difference.
        // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
        var expandedBounds = worldAABB;

        for (var i = 0; i < state.count; i++)
        {
            expandedBounds = expandedBounds.ExtendToContain(_lightsToRenderList[i].pos);
        }

        _debugStats.TotalLights += state.count;
        _debugStats.ShadowLights += Math.Min(state.shadowCastingCount, _maxShadowcastingLights);

        pass.SetClearColor(CompOrNull<MapLightComponent>(ev.Map)?.AmbientLightColor);

        ev.Passes.Add(pass);
    }

    private static bool LightQuery(ref (
            Clyde clyde,
            int count,
            int shadowCastingCount,
            EntityQuery<TransformComponent> xforms,
            Box2 worldAABB) state,
        in ComponentTreeEntry<PointLightComponent> value)
    {
        ref var count = ref state.count;
        ref var shadowCount = ref state.shadowCastingCount;

        // If there are too many lights, exit the query
        if (count >= state.clyde._maxLights)
            return false;

        var (light, transform) = value;
        var (lightPos, rot) = state.clyde._transformSystem.GetWorldPositionRotation(transform, state.xforms);
        lightPos += rot.RotateVec(light.Offset);
        var circle = new Circle(lightPos, light.Radius);

        // If the light doesn't touch anywhere the camera can see, it doesn't matter.
        // The tree query is not fully accurate because the viewport may be rotated relative to a grid.
        if (!circle.Intersects(state.worldAABB))
            return true;

        // If the light is a shadow casting light, keep a separate track of that
        if (light.CastShadows)
            shadowCount++;

        var distanceSquared = (state.worldAABB.Center - lightPos).LengthSquared();
        state.clyde._lightsToRenderList[count++] = (light, lightPos, distanceSquared, rot);

        return true;
    }
}
