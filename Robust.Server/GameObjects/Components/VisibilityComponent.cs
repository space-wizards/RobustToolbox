using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [Friend(typeof(VisibilitySystem))]
    public sealed class VisibilityComponent : Component
    {
        /// <summary>
        ///     The visibility layer for the entity.
        ///     Players whose visibility masks don't match this won't get state updates for it.
        /// </summary>
        [DataField("layer")]
        public int Layer = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public int LayerVV
        {
            get => Layer;
            set => EntitySystem.Get<VisibilitySystem>().SetLayer(this, value);
        }
    }
}
