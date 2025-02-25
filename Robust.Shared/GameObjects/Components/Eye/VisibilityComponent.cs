using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Controls PVS visibility of entities. THIS COMPONENT CONTROLS WHETHER ENTITIES ARE NETWORKED TO PLAYERS
    /// AND SHOULD NOT BE USED AS THE SOLE WAY TO HIDE AN ENTITY FROM A PLAYER.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SharedVisibilitySystem))]
    public sealed partial class VisibilityComponent : Component
    {
        /// <summary>
        ///     The visibility layer for the entity.
        ///     Players whose visibility masks don't match this won't get state updates for it.
        /// </summary>
        [DataField("layer")]
        public ushort Layer = 1;
    }
}
