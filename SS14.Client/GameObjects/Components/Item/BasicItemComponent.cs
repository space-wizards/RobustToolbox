using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Item;

namespace SS14.Client.GameObjects
{
    public class BasicItemComponent : Component
    {
        public Entity Holder;
        public InventoryLocation HoldingHand;

        public BasicItemComponent()
        {
            Family = ComponentFamily.Item;
        }

        public override System.Type StateType
        {
            get { return typeof (ItemComponentState); }
        }

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(ItemComponentState state)
        {
            if(state.Holder != null && (Holder == null || Holder.Uid != state.Holder))
            {
                Holder = Owner.EntityManager.GetEntity((int)state.Holder);
                HoldingHand = state.InventoryLocation;
            }
            if(Holder != null && state.Holder == null)
            {
                Holder = null;
                HoldingHand = state.InventoryLocation;
            }
        }
    }
}
