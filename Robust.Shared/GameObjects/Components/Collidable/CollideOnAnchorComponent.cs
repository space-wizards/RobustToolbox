using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// A component that leads to an entity being non-collidable upon being anchored and collidable upon beeing un-anchored.
    /// </summary>
    [RegisterComponent]
    public sealed class CollideOnAnchorComponent : Component
    {
        public override string Name => "CollideOnAnchor";

        /// <summary>
        /// Whether we toggle collision on or off when anchoring (and vice versa when unanchoring).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enable")]
        public bool Enable { get; set; } = false;
    }
}
