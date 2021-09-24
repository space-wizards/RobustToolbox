using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    public class VisibilityComponent : Component
    {
        [DataField("layer")]
        private int _layer = 1;
        public override string Name => "Visibility";

        /// <summary>
        ///     The visibility layer for the entity.
        ///     Players whose visibility masks don't match this won't get state updates for it.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Layer
        {
            get => _layer;
            set => _layer = value;
        }
    }
}
