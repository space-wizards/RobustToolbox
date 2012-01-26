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
    public class HealthComponent : DamageableComponent //Behaves like the damageable component but recieves updates about its health.
    {                                                  //Useful for objects that need to show different stages of damage clientside.
        protected float health = 0f;
        protected float maxHealth = 0f;

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
                   maxHealth = (float)message.messageParameters[2];
                   if (GetHealth() <= 0) isDead = true;
                   break;
            }
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentLocationDamage, GetHealth(), GetMaxHealth());
                    replies.Add(reply2);
                    break;
            }
        }

        public virtual float GetMaxHealth()
        {
            return maxHealth;
        }

        public virtual float GetHealth()
        {
            return health;
        }
    }
}
