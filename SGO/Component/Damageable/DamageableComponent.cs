using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class DamageableComponent : GameObjectComponent
    {
        private readonly List<DamageHistoryItem> _damageHistory = new List<DamageHistoryItem>();
        public float currentHealth = 100;
        
        protected bool isDead;
        public float maxHealth = 100;

        public DamageableComponent()
        {
            family = ComponentFamily.Damageable;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Damage:
                    ApplyDamage((Entity) list[0], (int) list[1], (DamageType) list[2]);
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    var reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(),
                                                           GetMaxHealth());
                    reply = reply2;
                    break;
            }

            return reply;
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

                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                                  client != null ? client : null, ComponentMessageType.HealthStatus,
                                                  isDead);
            }
        }

        protected virtual void ApplyDamage(IEntity damager, int damageamount, DamageType damType)
        {
            if (!isDead)
            {
                int damagetoapply = Math.Max(damageamount - GetArmor(damType), 0); //No negative damage right now
                currentHealth -= damagetoapply;
                DamagedBy(damager, damageamount, damType);
            }
            SendHealthUpdate();
        }

        protected virtual int GetArmor(DamageType damType)
        {
            EntityStatsComp entStats = (EntityStatsComp)Owner.GetComponent(ComponentFamily.EntityStats);

            if (entStats != null) return entStats.GetArmorValue(damType);
            else return 0;
        }

        protected virtual void ApplyDamage(int p)
        {
            ApplyDamage(null, p, DamageType.Untyped);
        }

        protected virtual void Die()
        {
            if (!isDead) isDead = true;

            //Send a message that whatever last damaged us killed us. 
            _damageHistory.Last().Damager.SendMessage(this, ComponentMessageType.KilledEntity, this);

            Owner.SendMessage(this, ComponentMessageType.Die);
        }

        protected void DamagedBy(IEntity damager, int amount, DamageType damType)
        {
            _damageHistory.Add(new DamageHistoryItem(damager, amount, damType));
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch(parameter.MemberName)
            {
                case "MaxHealth":
                    maxHealth = int.Parse((string) parameter.Parameter);
                    currentHealth = maxHealth;
                    break;
            }
        }
    }

    public struct DamageHistoryItem
    {
        public int Amount;
        public DamageType DamType;
        public IEntity Damager;
        public DateTime When;

        public DamageHistoryItem(IEntity damager, int amount, DamageType damType)
        {
            Damager = damager;
            Amount = amount;
            DamType = damType;
            When = DateTime.Now;
        }
    }
}
