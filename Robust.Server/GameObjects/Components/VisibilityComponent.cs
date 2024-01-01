using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [Access(typeof(VisibilitySystem))]
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
