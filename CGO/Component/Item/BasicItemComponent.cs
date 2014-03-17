using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Item;

namespace CGO
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

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            if (message.ComponentFamily != Family)
                return;
            switch ((ItemComponentNetMessage) message.MessageParameters[0])
            {
                case ItemComponentNetMessage.PickedUp: //I've been picked up -- says the server's item component
                    Entity e = Owner.EntityManager.GetEntity((int) message.MessageParameters[1]);
                    var h = (Hand) message.MessageParameters[2];
                    Owner.SendMessage(this, ComponentMessageType.PickedUp, h);
                    break;
                case ItemComponentNetMessage.Dropped: //I've been dropped -- says the server's item component
                    Owner.AddComponent(ComponentFamily.Mover,
                                       Owner.EntityManager.ComponentFactory.GetComponent("BasicMoverComponent"));
                    Owner.SendMessage(this, ComponentMessageType.Dropped);
                    break;
            }
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