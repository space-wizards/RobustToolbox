using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using SS3D_shared;
using System.Drawing;
using ClientServices;
using ClientInterfaces;

namespace CGO
{
    public class HealthComponent : DamageableComponent
    {
        float health = 100f;

        public HealthComponent()
            :base()
        {
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    health = (float)message.messageParameters[1];
                   break;
            }
        }

        public float GetHealth()
        {
            return health;
        }

    }
}
