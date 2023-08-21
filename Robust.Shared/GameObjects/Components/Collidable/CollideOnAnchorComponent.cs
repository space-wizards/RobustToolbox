using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// A component that toggles collision on an entity being toggled.
    /// </summary>
    [RegisterComponent]
    public sealed class CollideOnAnchorComponent : Component
    {
        /// <summary>
        /// Whether we toggle collision on or off when anchoring (and vice versa when unanchoring).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enable")]
        public bool Enable { get; set; } = false;
    }
}
