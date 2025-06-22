using System;
using JetBrains.Annotations;

namespace Robust.Client.Placement.Modes;

[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(PlacementMode))]
[MeansImplicitUse]
public sealed class PlacementModeAttribute(string? name = null) : Attribute
{
    /// <summary>
    /// The display name of this placement mode.
    /// </summary>
    public string? Name { get; } = name;
}
