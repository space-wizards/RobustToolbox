using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

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
}
