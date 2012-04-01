using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using ServerServices;
using ServerInterfaces;
using Lidgren.Network;

namespace SGO
{
    public class HealthComponent : DamageableComponent
    {
        public HealthComponent()
            : base()
        {
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    SendHealthUpdate(client);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
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
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, client != null ? client : null, ComponentMessageType.HealthStatus, health, maxHealth);
        }
    }
}
