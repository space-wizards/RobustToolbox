using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    /// <summary>
    /// Recieves movement data from the server and updates the entity's position accordingly.
    /// </summary>
    public class NetworkMoverComponent : GameObjectComponent
    {
        public NetworkMoverComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Mover;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            double x = (double)message.messageParameters[0];
            double y = (double)message.messageParameters[1];
            Translate((float)x, (float)y);
        }

        private void Translate(float x, float y)
        {
            Owner.position.X = x;
            Owner.position.Y = y;
        }
    }
}
