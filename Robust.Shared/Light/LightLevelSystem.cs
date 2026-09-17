using System;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Light;

/// <summary>
/// This system provides methods for computing the light level at some point in space. This is intended to
/// generally match the light values that would be computed by the default light shader.
/// </summary>
/// <remarks>
/// Note that the server and client might disagree about the computed light levels if there are any non-networked lights
/// or lights with client-side animations.
/// </remarks>
public sealed partial class LightLevelSystem : EntitySystem
{
    private float _maxLightRadius;

    private const float LightHeight = 1.0f;

    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SharedLightTreeSystem _tree = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CVars.MaxLightRadius, v => _maxLightRadius = v, true);
    }

    /// <summary>
    /// Compute the light level at an entity's position.
    /// </summary>
    /// <remarks>
    /// Note that the server and client might disagree about the computed light levels if there are any non-networked
    /// lights or lights with client-side animations.
    /// </remarks>
    public float CalculateLightLevel(EntityUid uid)
        => CalculateLightLevel(_transform.GetMapCoordinates(uid));

    /// <summary>
    /// Compute the light level at the given coordinates.
    /// </summary>
    /// <remarks>
    /// Note that the server and client might disagree about the computed light levels if there are any non-networked
    /// lights or lights with client-side animations.
    /// </remarks>
    public float CalculateLightLevel(EntityCoordinates point)
        => CalculateLightLevel(_transform.ToMapCoordinates(point));

    /// <inheritdoc cref="CalculateLightLevel(EntityCoordinates)"/>
    public float CalculateLightLevel(MapCoordinates point)
        => TryCalculateLightLevel(point, out var level) ? level : 0f;

    public bool TryCalculateLightLevel(EntityUid uid, out float level, LightLevelQueryOptions options = default)
        => TryCalculateLightLevel(_transform.GetMapCoordinates(uid), out level, options);

    public bool TryCalculateLightLevel(EntityCoordinates point, out float level, LightLevelQueryOptions options = default)
        => TryCalculateLightLevel(_transform.ToMapCoordinates(point), out level, options);

    public bool TryCalculateLightLevel(MapCoordinates point, out float level, LightLevelQueryOptions options = default)
    {
        if (!TryCalculateLightColor(point, out var color, options))
        {
            level = default;
            return false;
        }

        level = ColorToLevel(color);
        return true;
    }

    /// <summary>
    /// Convert from a total light color to a single "brightness/intensity" float.
    /// </summary>
    public float ColorToLevel(Color color)
    {
        // TODO: Colorspace-specific structs I beg, this is linear.
        var luminance = 0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;
        return Math.Clamp(luminance, 0f, 1f);
    }

    /// <inheritdoc cref="CalculateLightLevel(EntityUid)"/>
    public Color CalculateLightColor(EntityUid uid)
        => CalculateLightColor(_transform.GetMapCoordinates(uid));

    /// <inheritdoc cref="CalculateLightLevel(EntityCoordinates)"/>
    public Color CalculateLightColor(EntityCoordinates point)
        => CalculateLightColor(_transform.ToMapCoordinates(point));

    /// <inheritdoc cref="CalculateLightLevel(EntityCoordinates)"/>
    public Color CalculateLightColor(MapCoordinates point)
        => TryCalculateLightColor(point, out var color) ? color : Color.Black;

    /// <summary>
    /// Try to compute additive light colour at the given coordinates.
    /// </summary>
    /// <remarks>
    /// This includes map ambient light. If map lighting is disabled, this returns fully-lit white because the renderer
    /// skips the lighting pass for that map. Client-only clear-color overrides are not represented in shared state, so
    /// this can differ from a client viewport that overrides the lighting clear color locally.
    /// </remarks>
    public bool TryCalculateLightColor(EntityUid uid, out Color color, LightLevelQueryOptions options = default)
        => TryCalculateLightColor(_transform.GetMapCoordinates(uid), out color, options);

    public bool TryCalculateLightColor(EntityCoordinates point, out Color color, LightLevelQueryOptions options = default)
        => TryCalculateLightColor(_transform.ToMapCoordinates(point), out color, options);

    public bool TryCalculateLightColor(MapCoordinates point, out Color color, LightLevelQueryOptions options = default)
    {
        if (!TryGetAmbientLight(point, out color, out var lightingEnabled))
            return false;

        if (!lightingEnabled)
            return true;

        if (!_tree.IsAvailable)
            return false;

        var pos = point.Position;
        var treeSearchAabb = new Box2(pos, pos).Enlarged(_maxLightRadius);
        var lights = new ValueList<Light>();

        // We manually do a tree lookup instead of using LightTreeSystem.QueryAabb
        // This is because the actual area we want to query for intersecting lights is a point, but we want to include trees from further away.
        foreach (var (tree, treeComp) in _tree.GetIntersectingTrees(point.MapId, treeSearchAabb))
        {
            var localPos = Vector2.Transform(pos, _transform.GetInvWorldMatrix(tree));
            treeComp.Tree.QueryPoint(ref lights, options.ShadowCastingOnly ? ShadowcastingCallback : AllLightCallback, localPos, true);
        }

        // Compute light positions, and get the maximum radius
        var lightSpan = lights.Span;
        var maxRadius = 0f;
        var maxShadowRadius = 0f;
        ComputeLightPositions(lightSpan, ref maxRadius);

        foreach (ref var light in lightSpan)
        {
            if (light.Entity.Comp1.CastShadows)
                maxShadowRadius = Math.Max(Math.Min(light.Entity.Comp1.Radius, _maxLightRadius), maxShadowRadius);
        }

        if (maxShadowRadius == 0f)
        {
            AddUnoccludedLights(pos, lightSpan, ref color);
            return true;
        }

        // Use the max radius to look for any occluder trees. This could be handled better by only using a Box2 that
        // contains the centre point of all lights, which would allow us to use the HandleSingleOccluder branch more,
        // but this approximation is probably fine most of the time.
        var occluderAabb = new Box2(pos, pos).Enlarged(maxShadowRadius);
        var occluderTrees = _occluder.GetIntersectingTreesInternal(point.MapId, occluderAabb);

        // Most of the time, there will probably only be one occluder tree in range
        var lightColor = occluderTrees.Count == 1
            ? HandleSingleOccluder(pos, lightSpan, occluderTrees[0])
            : HandleMultipleOccluders(pos, lightSpan, occluderTrees.Span);
        color = new Color(color.RGBA + lightColor.RGBA);
        return true;

        static bool ShadowcastingCallback(ref ValueList<Light> lights, in ComponentTreeEntry<SharedPointLightComponent> value)
        {
            if (value.Component.CastShadows)
                lights.Add(new(value));
            return true;
        }

        static bool AllLightCallback(ref ValueList<Light> lights, in ComponentTreeEntry<SharedPointLightComponent> value)
        {
            lights.Add(new(value));
            return true;
        }
    }

    private void ComputeLightPositions(Span<Light> lights, ref float maxRadius)
    {
        foreach (ref var light in lights)
        {
            (light.Position, light.Rotation) = _transform.GetWorldPositionRotation(light.Entity.Comp2);
            light.Position += light.Rotation.RotateVec(light.Entity.Comp1.Offset);
            maxRadius = Math.Max(Math.Min(light.Entity.Comp1.Radius, _maxLightRadius), maxRadius);
        }
    }

    private bool TryGetAmbientLight(MapCoordinates point, out Color color, out bool lightingEnabled)
    {
        if (!_map.TryGetMap(point.MapId, out var mapUid) || !TryComp(mapUid, out MapComponent? map))
        {
            color = default;
            lightingEnabled = false;
            return false;
        }

        lightingEnabled = map.LightingEnabled;
        if (!map.LightingEnabled)
        {
            color = Color.White;
            return true;
        }

        color = CompOrNull<MapLightComponent>(mapUid)?.AmbientLightColor ?? MapLightComponent.DefaultColor;
        return true;
    }

    private void AddUnoccludedLights(Vector2 pos, Span<Light> lights, ref Color color)
    {
        var colorVec = color.RGBA;

        foreach (ref var entry in lights)
        {
            var delta = pos - entry.Position;
            if (InRange(entry.Entity.Comp1.Radius, delta))
                colorVec += GetColourFromLight(entry.Entity.Comp1, delta, entry.Rotation);
        }

        color = new Color(colorVec);
    }

    private Color HandleSingleOccluder(Vector2 pos, Span<Light> lights, Entity<OccluderTreeComponent> tree)
    {
        var (_, rot, mat) = _transform.GetWorldPositionRotationInvMatrix(tree.Owner);
        rot = -rot;

        var color = Vector4.Zero;
        foreach (ref var entry in lights)
        {
            var delta = pos - entry.Position;
            if (!InRange(entry.Entity.Comp1.Radius, delta))
                continue;

            if (!entry.Entity.Comp1.CastShadows ||
                Unoccluded(entry.Position, delta, tree.Comp, in mat, rot))
                color += GetColourFromLight(entry.Entity.Comp1, delta, entry.Rotation);
        }

        return new Color(color);
    }

    private Color HandleMultipleOccluders(
        Vector2 pos,
        Span<Light> lightSpan,
        Span<(EntityUid Uid, OccluderTreeComponent Comp)> trees)
    {
        var occluderXforms = trees.Length < 16
            ? stackalloc OccluderTransform[trees.Length]
            : new OccluderTransform[trees.Length];

        for (var i = 0; i < trees.Length; i++)
        {
            var (_, rot, mat) = _transform.GetWorldPositionRotationInvMatrix(trees[i].Uid);
            occluderXforms[i] = new(-rot, mat);
        }

        var color = Vector4.Zero;
        foreach (ref var entry in lightSpan)
        {
            var delta = pos - entry.Position;
            if (!InRange(entry.Entity.Comp1.Radius, delta))
                continue;

            if (!entry.Entity.Comp1.CastShadows ||
                Unoccluded(entry.Position, delta, trees, occluderXforms))
                color += GetColourFromLight(entry.Entity.Comp1, delta, entry.Rotation);
        }

        return new Color(color);
    }

    private bool InRange(float radius, Vector2 delta)
    {
        var cappedRadius = Math.Min(radius, _maxLightRadius);
        return delta.LengthSquared() <= cappedRadius * cappedRadius;
    }

    private static bool Unoccluded(
        Vector2 lightPos,
        Vector2 delta,
        ReadOnlySpan<(EntityUid, OccluderTreeComponent)> trees,
        Span<OccluderTransform> treeXforms)
    {
        var length = delta.Length();
        if (MathHelper.CloseTo(length, 0))
            return true;

        var normalized = delta / length;

        (bool Hit, float Length) state = (false, length);
        for (var i = 0; i < trees.Length; i++)
        {
            var relativeAngle = treeXforms[i].Rotation.RotateVec(normalized);
            var treeRay = new Ray(Vector2.Transform(lightPos, treeXforms[i].Matrix), relativeAngle);
            trees[i].Item2.Tree.QueryRay(ref state, Callback, treeRay);
            if (state.Hit)
                return false;
        }

        return true;
    }

    private static bool Unoccluded(
        Vector2 lightPos,
        Vector2 delta,
        OccluderTreeComponent tree,
        in Matrix3x2 treeXform,
        Angle treeRot)
    {
        var length = delta.Length();
        if (MathHelper.CloseTo(length, 0))
            return true;

        var normalized = delta / length;

        var relativeAngle = treeRot.RotateVec(normalized);
        var treeRay = new Ray(Vector2.Transform(lightPos, treeXform), relativeAngle);
        (bool Hit, float Length) state = (false, length);
        tree.Tree.QueryRay(ref state, Callback, treeRay);
        return !state.Hit;
    }

    private static bool Callback(ref (bool Hit, float Range) state, in ComponentTreeEntry<OccluderComponent> _, in Vector2 __, float dist)
    {
        if (dist > state.Range)
            return true;
        state.Hit = true;
        return false;
    }

    private Vector4 GetColourFromLight(SharedPointLightComponent light, Vector2 distance, Angle worldRotation)
    {
        // Calculate the light level the same way as in light_shared.swsl.
        var radius = Math.Min(light.Radius, _maxLightRadius);
        var sqrtDist = Vector2.Dot(distance, distance) + LightHeight;
        var s = Math.Clamp(MathF.Sqrt(sqrtDist) / radius, 0.0f, 1.0f);
        var s2 = s * s;
        var curveFactor = MathHelper.Lerp(s, s2, Math.Clamp(light.CurveFactor, 0.0f, 1.0f));
        var lightVal = Math.Clamp(((1.0f - s2) * (1.0f - s2)) / (1.0f + light.Falloff * curveFactor), 0.0f, 1.0f);
        var finalLightVal = light.Color.RGBA * (light.Energy * lightVal);

        if (!_proto.TryIndex(light.LightMask, out var mask))
            return finalLightVal;

        var maskRot = SharedPointLightSystem.GetMaskWorldRotation(light, worldRotation);
        var relativeAngle = MathHelper.CloseTo(distance.LengthSquared(), 0)
            ? Angle.Zero
            : Angle.FromWorldVec(distance) - maskRot;

        // TODO LIGHTLEVEL read light mask
        // read the mask image into a buffer of pixels and sample the returned color to multiply against the light level before final calculation
        // var stream = _resource.ContentFileRead(mask.MaskPath);
        // var image = Image.Load<Rgba32>(stream);
        // Rgba32[] pixelArray = new Rgba32[image.Width * image.Height];
        // image.CopyPixelDataTo(pixelArray);

        var calculatedLight = 0d;
        foreach (var cone in mask.LightCones)
        {
            var delta = Math.Abs(Angle.ShortestDistance(relativeAngle, cone.Direction));

            // Target is outside the cone's outer width angle, so ignore
            if (delta > cone.OuterWidth)
                continue;

            // Target is within the inner cone, return the full color
            if (delta < cone.InnerWidth)
                return finalLightVal;

            // Lerp light from 0 to 1 as angle goes from outer to inner.
            // Not additive because multiple cones for the same mask don't work like that.
            calculatedLight = Math.Max(calculatedLight, (cone.OuterWidth - delta) / (cone.OuterWidth - cone.InnerWidth));
        }

        return finalLightVal * MathF.Min(1, (float)calculatedLight);
    }

    private record struct OccluderTransform(Angle Rotation, Matrix3x2 Matrix);

    private record struct Light(
        Entity<SharedPointLightComponent, TransformComponent> Entity,
        Vector2 Position = default,
        Angle Rotation = default);
}

public readonly record struct LightLevelQueryOptions(bool ShadowCastingOnly = false);
