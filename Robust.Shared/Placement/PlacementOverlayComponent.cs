using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Robust.Shared.Placement;

/// <summary>
///    That component allows modifying the overlay used by the placement system.
/// </summary>
[RegisterComponent]
public sealed partial class PlacementOverlayComponent : Component
{
    /// <summary>
    ///     Specific sprite for the placement overlay.
    /// </summary>
    [DataField(readOnly: true, required: true)] public SpriteSpecifier Sprite;

    /// <summary>
    ///     Whether the placement overlay should rotate (default: false).
    /// </summary>
    [DataField(readOnly: true)] public bool NoRotation;

    /// <summary>
    ///     Scaling vector for the placement overlay (default: Vector2.One).
    /// </summary>
    [DataField(readOnly: true)] public Vector2 Scale = Vector2.One;
}
