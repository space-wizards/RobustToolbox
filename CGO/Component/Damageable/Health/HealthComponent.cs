using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class HealthComponent : DamageableComponent //Behaves like the damageable component but recieves updates about its health.
    {                                                  //Useful for objects that need to show different stages of damage clientside.
        protected float Health ;
        protected float MaxHealth;

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    Health = (float)message.MessageParameters[1];
                    MaxHealth = (float)message.MessageParameters[2];
                    if (GetHealth() <= 0) Die();
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
                    break;
            }

            return reply;
        }

        public virtual float GetMaxHealth()
        {
            return MaxHealth;
        }

        public virtual float GetHealth()
        {
            return Health;
        }
    }
}
