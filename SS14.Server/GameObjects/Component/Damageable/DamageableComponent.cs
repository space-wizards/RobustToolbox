using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class DamageableComponent : Component
    {
        public override string Name => "Damageable";
        private readonly List<DamageHistoryItem> _damageHistory = new List<DamageHistoryItem>();
        public float currentHealth = 100;

        protected bool isDead;
        public float maxHealth = 100;

        public DamageableComponent()
        {
            Family = ComponentFamily.Damageable;
            RegisterSVar("MaxHealth", typeof(int));
            RegisterSVar("CurrentHealth", typeof(int));
        }

        // TODO use state system
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
                    ApplyDamage((IEntity)list[0], (int)list[1], (DamageType)list[2]);
                    break;
            }

            return reply;
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

        [Obsolete("This is old and should be removed. Everything that calls this should instead trigger Die() if necessary.")]
        protected virtual void SendHealthUpdate(NetConnection client)
        {
            if (currentHealth <= 0)
            {
                if (isDead == false)
                {
                    Die();
                }
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
            var entStats = (EntityStatsComp)Owner.GetComponent(ComponentFamily.EntityStats);

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
            else
            {
                return;
            }

            //Send a message that whatever last damaged us killed us.
            _damageHistory.Last().Damager.SendMessage(this, ComponentMessageType.KilledEntity, this);

            Owner.SendMessage(this, ComponentMessageType.Die);
        }

        protected void DamagedBy(IEntity damager, int amount, DamageType damType)
        {
            _damageHistory.Add(new DamageHistoryItem(damager, amount, damType));
        }

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            YamlNode node;
            if (mapping.TryGetValue("maxHealth", out node))
            {
                maxHealth = node.AsInt();
                currentHealth = maxHealth;
            }

            if (mapping.TryGetValue("currentHealth", out node))
            {
                currentHealth = node.AsInt();
            }
        }

        public override IList<ComponentParameter> GetParameters()
        {
            IList<ComponentParameter> cparams = base.GetParameters();
            cparams.Add(new ComponentParameter("MaxHealth", (int)maxHealth));
            cparams.Add(new ComponentParameter("CurrentHealth", (int)currentHealth));
            return cparams;
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
