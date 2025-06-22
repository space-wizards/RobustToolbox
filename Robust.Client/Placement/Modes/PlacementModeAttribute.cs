using System;
using JetBrains.Annotations;

namespace Robust.Client.Placement.Modes;

[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(PlacementMode))]
[MeansImplicitUse]
public sealed class PlacementModeAttribute(string? name = null, int priority = 0) : Attribute
{
    /// <summary>
    /// The display name of this placement mode.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Controls ordering of modes in the selector. Higher priority modes are listed first.
    /// </summary>
    public int Priority { get; } = priority;
}

public enum EnginePlacementMode
{
    PlaceFree = 1000,
    PlaceNearby = 999,
    SnapgridCenter = 998,
    SnapgridBorder = 997,
    AlignSimilar = 996,
    AlignTileAny = 995,
    AlignTileEmpty = 994,
    AlignTileNonDense = 993,
    AlignTileDense = 992,
    AlignWall = 991,
    AlignWallProper = 990,
}
