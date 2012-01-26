using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;
using ServerServices;
using ServerInterfaces;
using Lidgren.Network;

namespace SGO
{
    public class HumanHealthComponent : HealthComponent
    {
        protected List<DamageLocation> damageZones = new List<DamageLocation>();

        public HumanHealthComponent()
            : base()
        {
            damageZones.Add(new DamageLocation(BodyPart.arm_l, 50));
            damageZones.Add(new DamageLocation(BodyPart.arm_r, 50));
            damageZones.Add(new DamageLocation(BodyPart.groin, 50));
            damageZones.Add(new DamageLocation(BodyPart.head, 50));
            damageZones.Add(new DamageLocation(BodyPart.leg_l, 50));
            damageZones.Add(new DamageLocation(BodyPart.leg_r, 50));
            damageZones.Add(new DamageLocation(BodyPart.torso, 100));

            this.maxHealth = damageZones.Sum(x => x.maxHealth);
            this.currentHealth = this.maxHealth;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected void ApplyDamage(Entity damager, int damageamount, DamageType damType, BodyPart targetLocation)
        {
            if (damageZones.Exists(x => x.location == targetLocation))
            {
                DamageLocation dmgLoc = damageZones.First(x => x.location == targetLocation);
                dmgLoc.AddDamage(damType, damageamount - GetArmorValue(damType));
            }

            currentHealth = GetHealth();
            maxHealth = GetMaxHealth();

            SendHealthUpdate();
        }

        protected override void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            ApplyDamage(damager, damageamount, damType, BodyPart.torso); //Apply randomly instead of chest only
        }

        protected override void ApplyDamage(int p)
        {
            ApplyDamage(Owner, p, DamageType.Untyped, BodyPart.torso); ; //Apply randomly instead of chest only
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

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetCurrentLocationHealth:
                    BodyPart location = (BodyPart)list[0];
                    if (damageZones.Exists(x => x.location == location))
                    {
                        DamageLocation dmgLoc = damageZones.First(x => x.location == location);
                        ComponentReplyMessage reply1 = new ComponentReplyMessage(ComponentMessageType.CurrentLocationHealth, location, dmgLoc.UpdateTotalHealth(), dmgLoc.maxHealth);
                        replies.Add(reply1);
                    }
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
                    replies.Add(reply2);
                    break;
                case ComponentMessageType.Damage:
                    if(list.Count() > 3) //We also have a target location
                        ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2], (BodyPart)list[3]);
                    else//We dont have a target location
                        ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2]);
                    break;
                default:
                    base.RecieveMessage(sender, type, replies, list);
                    break;
            }
        }

        public override float GetMaxHealth()
        {
            return damageZones.Sum(x => x.maxHealth);
        }

        public override float GetHealth()
        {
            return damageZones.Sum(x => x.UpdateTotalHealth());
        }

        protected override void SendHealthUpdate()
        {
            SendHealthUpdate(null);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        protected override void SendHealthUpdate(NetConnection client)
        {
            foreach (DamageLocation loc in damageZones)
            {
                List<object> newUp = new List<object>();
                newUp.Add(ComponentMessageType.HealthStatus);
                newUp.Add(loc.location);
                newUp.Add(loc.damageIndex.Count);
                newUp.Add(loc.maxHealth);
                foreach (KeyValuePair<DamageType, int> damagePair in loc.damageIndex)
                {
                    newUp.Add(damagePair.Key);
                    newUp.Add(damagePair.Value);
                }
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, client != null ? client : null, newUp.ToArray());
            }
        }
    }
}
