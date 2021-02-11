using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    public class VisibilityComponent : Component
    {
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _layer, "layer", 1);
        }
    }
}
