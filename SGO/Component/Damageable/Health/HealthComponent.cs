using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
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

        public override void  Update(float frameTime)
        {
 	        base.Update(frameTime);
        }

        protected override void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            base.ApplyDamage(damager, damageamount, damType);
            SendHealthUpdate();
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
