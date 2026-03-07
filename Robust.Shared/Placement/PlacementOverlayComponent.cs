using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Placement
{
    [RegisterComponent]
    public sealed partial class PlacementOverlayComponent : Component
    {        /// <summary>
             ///     A SpriteSpecifier that will be used while in placement mode for this prototype
             /// </summary>
        [DataField(readOnly: true, required: true)] public SpriteSpecifier sprite;
    }
}
