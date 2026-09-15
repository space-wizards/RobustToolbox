using System;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Shared.Map;

/// <summary>
/// Shared z-level projection arithmetic. Simulation and physics coordinates are always unprojected.
/// </summary>
public static class ZLevelProjection
{
    public const float BoundaryEpsilon = 0.0001f;

    public static float GetAbsoluteZ(int mapDepth, float localHeight)
        => mapDepth + localHeight;

    public static float GetLocalHeight(float absoluteZ, int mapDepth)
        => absoluteZ - mapDepth;

    public static Vector2 Project(
        Vector2 canonicalPosition,
        float absoluteZ,
        int referenceDepth,
        Vector2 projectionOffset)
        => canonicalPosition + projectionOffset * (absoluteZ - referenceDepth);

    public static Vector2 Unproject(
        Vector2 projectedPosition,
        float absoluteZ,
        int referenceDepth,
        Vector2 projectionOffset)
        => projectedPosition - projectionOffset * (absoluteZ - referenceDepth);

    /// <summary>
    /// Projects a canonical support/contact point at an absolute height into a viewed z-map plane.
    /// </summary>
    public static Vector2 ProjectSupportPoint(
        Vector2 canonicalContactPoint,
        float absoluteSupportHeight,
        int viewedMapDepth,
        Vector2 projectionOffset)
        => Project(canonicalContactPoint, absoluteSupportHeight, viewedMapDepth, projectionOffset);

    /// <summary>
    /// Reverses <see cref="ProjectSupportPoint"/> for picking a surface at a known absolute height.
    /// </summary>
    public static Vector2 UnprojectSupportPoint(
        Vector2 projectedContactPoint,
        float absoluteSupportHeight,
        int viewedMapDepth,
        Vector2 projectionOffset)
        => Unproject(projectedContactPoint, absoluteSupportHeight, viewedMapDepth, projectionOffset);

    public static Vector2 Reproject(
        Vector2 projectedPosition,
        int fromReferenceDepth,
        int toReferenceDepth,
        Vector2 projectionOffset)
        => projectedPosition + projectionOffset * (fromReferenceDepth - toReferenceDepth);

    /// <summary>
    /// Camera offset that makes a discrete map layer appear at its configured projected depth.
    /// </summary>
    public static Vector2 GetLayerEyeOffset(int relativeDepth, Vector2 projectionOffset)
        => -projectionOffset * relativeDepth;

    /// <summary>
    /// Splits an absolute z position into adjacent integer layers and complementary weights.
    /// Exact integer positions produce one full-opacity sample.
    /// </summary>
    public static ZLevelLayerWeights GetLayerWeights(float absoluteZ)
    {
        var nearest = MathF.Round(absoluteZ);
        if (MathF.Abs(absoluteZ - nearest) <= BoundaryEpsilon)
            return new((int) nearest, 1f, (int) nearest, 0f);

        var lower = (int) MathF.Floor(absoluteZ);
        var upperWeight = Math.Clamp(absoluteZ - lower, 0f, 1f);
        return new(lower, 1f - upperWeight, lower + 1, upperWeight);
    }
}

public readonly record struct ZLevelLayerWeights(
    int LowerDepth,
    float LowerWeight,
    int UpperDepth,
    float UpperWeight);
