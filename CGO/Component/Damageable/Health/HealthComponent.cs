using System;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Damageable.Health;

namespace CGO
{
    public class HealthComponent : DamageableComponent
        //Behaves like the damageable component but recieves updates about its health.
    {
        //Useful for objects that need to show different stages of damage clientside.
        protected float Health;
        protected float MaxHealth;

        public override Type StateType
        {
            get { return typeof (HealthComponentState); }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];

            /*switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    Health = (float)message.MessageParameters[1];
                    MaxHealth = (float)message.MessageParameters[2];
                    if (GetHealth() <= 0) Die();
                    break;
            }*/
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

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

        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((HealthComponentState) state);

            Health = state.Health;
            MaxHealth = state.MaxHealth;
        }
    }
}