using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    //Moves the entity based on input from a Clientside KeyBindingMoverComponent.
    public class PlayerInputMoverComponent : GameObjectComponent
    {
        public PlayerInputMoverComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Mover;
        }

        /// <summary>
        /// Handles position messages. that should be it.
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            bool shouldMove = true;
            if (Owner.HasComponent(ComponentFamily.StatusEffects))
            {
                StatusEffectComp statComp = (StatusEffectComp)Owner.GetComponent(ComponentFamily.StatusEffects);
                if (statComp.HasFamily(StatusEffectFamily.Root) || statComp.HasFamily(StatusEffectFamily.Stun)) shouldMove = false;
            }

            if (shouldMove) Translate(Convert.ToDouble((float)message.messageParameters[0]), Convert.ToDouble((float)message.messageParameters[1]));
            else SendPositionUpdate(true); //Tried to move even though they cant. Lets pin that fucker down.
        }

        public void Translate(double x, double y)
        {
            Vector2 oldPosition = Owner.position;
            Owner.position = new Vector2(x, y);
            Owner.Moved(oldPosition);
            SendPositionUpdate();
        }

        public void SendPositionUpdate(bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, Owner.position.X, Owner.position.Y, forced);
        }

        public void SendPositionUpdate(NetConnection client,bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, client, Owner.position.X, Owner.position.Y, forced);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendPositionUpdate(netConnection, true);
        }
    }
}
