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
    public class DamageableComponent : GameObjectComponent
    {
        public float maxHealth = 100;
        public float currentHealth = 100;

        protected bool isDead = false;

        public DamageableComponent()
            :base()
        {
            family = ComponentFamily.Damageable;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case SS3D_shared.GO.ComponentMessageType.Damage:
                    /// Who damaged, how much, what type
                    ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2]);
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentLocationDamage, GetHealth(), GetMaxHealth());
                    replies.Add(reply2);
                    break;
            }
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

        public virtual float GetMaxHealth()
        {
            return maxHealth;
        }

        public virtual float GetHealth()
        {
            return currentHealth;
        }

        protected virtual void SendHealthUpdate()
        {
            SendHealthUpdate(null);
        }

        protected virtual void SendHealthUpdate(NetConnection client)
        {
            if (currentHealth <= 0)
            {
                if (isDead == false)
                {
                    isDead = true;
                    Owner.SendMessage(this, ComponentMessageType.Die, null);
                }

                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, client != null ? client : null, ComponentMessageType.HealthStatus, isDead);
            }
        }

        protected virtual void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            if(!isDead)
            {
                int damagetoapply = Math.Max(damageamount - GetArmorValue(damType), 0); //No negative damage right now
                currentHealth -= damagetoapply;
            }
            SendHealthUpdate();
        }

        protected virtual int GetArmorValue(DamageType damType)
        {
            //TODO do armor by damagetype
            int armorvalues = 0;
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.GetArmorValues, replies, damType);
            foreach (ComponentReplyMessage reply in replies)
            {
                if (reply.messageType == ComponentMessageType.ReturnArmorValues && reply.paramsList[0].GetType() == typeof(int))
                {
                    armorvalues += (int)reply.paramsList[0];
                }
            }
            return armorvalues;
        }

        protected virtual void ApplyDamage(int p)
        {
            ApplyDamage(null, p, DamageType.Untyped);
        }
    }
}
