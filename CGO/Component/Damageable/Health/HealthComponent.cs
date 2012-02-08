using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO.Component.Damageable.Health
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
                    if (GetHealth() <= 0) IsDead = true;
                    break;
            }
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    var reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
                    replies.Add(reply2);
                    break;
                default:
                    base.RecieveMessage(sender, type, replies, list);
                    break;
            }
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
