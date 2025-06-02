using System;
using JetBrains.Annotations;

namespace Robust.Client.Placement.Modes;

/// <summary>
/// Registers this <see cref="PlacementMode"/> for use by the entity spawn panel.
/// </summary>
/// <param name="name">Override the display name of the mode. If null, the class name will be used.</param>
/// <param name="priority">Order of the mode in the list. Higher values are placed higher in the list.</param>
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
    PlaceFree = 1100,
    PlaceNearby = 1090,
    SnapgridCenter = 1080,
    SnapgridBorder = 1070,
    AlignSimilar = 1060,
    AlignTileAny = 1050,
    AlignTileEmpty = 1040,
    AlignTileNonDense = 1030,
    AlignTileDense = 1020,
    AlignWall = 1010,
    AlignWallProper = 1000,
}
