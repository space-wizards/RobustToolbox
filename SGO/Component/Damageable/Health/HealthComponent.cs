using System;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Damageable.Health;

namespace SGO
{
    public class HealthComponent : DamageableComponent
    {
        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected override void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            base.ApplyDamage(damager, damageamount, damType);
            SendHealthUpdate();
        }
        
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    SendHealthUpdate(client);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    var reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(),
                                                           GetMaxHealth());
                    reply = reply2;
                    break;
            }

            return reply;
        }

        protected override void ApplyDamage(int p)
        {
            base.ApplyDamage(p);
            SendHealthUpdate();
        }

        protected override void SendHealthUpdate()
        {
            SendHealthUpdate(null);
        }

        protected override void SendHealthUpdate(NetConnection client)
        {
            float health = GetHealth();
            /*Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, client != null ? client : null,
                                              ComponentMessageType.HealthStatus, health, maxHealth);*/
        }

        public override ComponentState GetComponentState()
        {
            return new HealthComponentState(isDead, GetHealth(), maxHealth);
        }
    }
}