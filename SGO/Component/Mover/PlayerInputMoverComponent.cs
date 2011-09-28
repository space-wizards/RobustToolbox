using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

namespace SGO
{
    //Moves the entity based on input from a Clientside KeyBindingMoverComponent.
    public class PlayerInputMoverComponent : GameObjectComponent
    {
        public PlayerInputMoverComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Mover;
        }

        /// <summary>
        /// Handles position messages. that should be it.
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            Translate(Convert.ToDouble((float)message.messageParameters[0]), Convert.ToDouble((float)message.messageParameters[1]));
        }

        public void Translate(double x, double y)
        {
            Owner.position = new Vector2(x, y);
            Owner.Moved();
            SendPositionUpdate();
        }

        public void SendPositionUpdate()
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, null, Owner.position.X, Owner.position.Y);
        }
    }
}
