using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics.Clyde;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed class ClientLightingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly LightTreeSystem _lightTree = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    private List<RenderLight> _lights = new();

    private LightComparer _comparer = new();

    private int _maxShadowcastingLights;
    private int _maxLights;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightingPassEvent>(OnLightingPass);
        Subs.CVar(_cfgManager, CVars.MaxLightCount, MaxLightsChanged, true);
        Subs.CVar(_cfgManager, CVars.MaxShadowcastingLights, MaxShadowLightsChanged, true);
    }

    private void MaxShadowLightsChanged(int value)
    {
        _maxShadowcastingLights = value;
        DebugTools.Assert(_maxLights >= _maxShadowcastingLights);
    }

    private void MaxLightsChanged(int value)
    {
        _maxLights = value;
        DebugTools.Assert(_maxLights >= _maxShadowcastingLights);
    }

    private void OnLightingPass(ref LightingPassEvent ev)
    {
        _lights.Clear();

        var pass = new LightingPass
        {
            Bind = true,
            Target = ev.Viewport.LightRenderTarget,
        };

        // Use worldbounds for this one as we only care if the light intersects our actual bounds
        var state = (this, _lights, count: 0, shadowCastingCount: 0, ev.WorldAabb);

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
            _lights.Sort(_comparer);

            // Then effectively delete the furthest lights, by setting the end of the array to exclude N
            // number of shadow casting lights (where N is the number above the max number per scene.)
            state.count -= state.shadowCastingCount - _maxShadowcastingLights;
        }

        // TODO: NO FORGETTI
        // _debugStats.TotalLights += state.count;
        // _debugStats.ShadowLights += Math.Min(state.shadowCastingCount, _maxShadowcastingLights);

        pass.SetClearColor(Color.FromSrgb(CompOrNull<MapLightComponent>(ev.Map)?.AmbientLightColor ?? Color.Black));

        pass.Lights = _lights;
        ev.Add(pass);
    }

    private sealed class LightComparer : IComparer<RenderLight>
    {
        public int MaxLights;
        public Vector2 WorldPos;

        public int Compare(RenderLight x, RenderLight y)
        {
            if (x.CastShadows != y.CastShadows)
                return x.CastShadows.CompareTo(y.CastShadows);

            return x.Distance.CompareTo(y.Distance);
        }
    }

    private static bool LightQuery(ref (
            ClientLightingSystem system,
            List<RenderLight> lights,
            int count,
            int shadowCastingCount,
            Box2 worldAABB) state,
        in ComponentTreeEntry<PointLightComponent> value)
    {
        ref var count = ref state.count;
        ref var shadowCount = ref state.shadowCastingCount;

        // If there are too many lights, exit the query
        if (count >= state.system._maxLights)
            return false;

        var (light, transform) = value;
        var (lightPos, rot) = state.system._xformSystem.GetWorldPositionRotation(transform);
        lightPos += rot.RotateVec(light.Offset);
        var circle = new Circle(lightPos, light.Radius);

        // If the light doesn't touch anywhere the camera can see, it doesn't matter.
        // The tree query is not fully accurate because the viewport may be rotated relative to a grid.
        if (!circle.Intersects(state.worldAABB))
            return true;

        // If the light is a shadow casting light, keep a separate track of that
        if (light.CastShadows)
            shadowCount++;

        state.lights.Add(new RenderLight()
        {
            Distance = (state.worldAABB.Center - lightPos).Length(),
            CastShadows = light.CastShadows,
            Color = light.Color,
            Energy = light.Energy,
            Mask = light.Mask,
            Offset = light.Offset,
            Position = lightPos,
            Radius = light.Radius,
            Rotation = rot,
            Softness = light.Softness,
            MaskAutoRotate = light.MaskAutoRotate,
        });

        return true;
    }
}
