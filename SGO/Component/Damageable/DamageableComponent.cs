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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Damage:
                    ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2]);
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
                    reply = reply2;
                    break;
            }

            return reply;
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
                    Die();
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
            var replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.GetArmorValues, replies, damType);
            return replies.Where(reply => reply.MessageType == ComponentMessageType.ReturnArmorValues && reply.ParamsList[0] is int).Sum(reply => (int) reply.ParamsList[0]);
        }

        protected virtual void ApplyDamage(int p)
        {
            ApplyDamage(null, p, DamageType.Untyped);
        }

        protected virtual void Die()
        {
            if (!isDead) isDead = true;

            Owner.SendMessage(this, ComponentMessageType.Die);
        }
    }
}
