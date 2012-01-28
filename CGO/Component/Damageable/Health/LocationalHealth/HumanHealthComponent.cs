using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using SS3D_shared;
using System.Drawing;
using ClientServices;
using ClientInterfaces;

namespace CGO
{
    public class HumanHealthComponent : HealthComponent //Behaves like health component but tracks damage of individual zones.
    {                                                   //Useful for mobs.

        public List<DamageLocation> damageZones = new List<DamageLocation>(); //makes this protected again.

        public HumanHealthComponent()
            :base()
        {
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                   HandleHealthUpdate(message);
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
                default:
                    base.RecieveMessage(sender, type, replies, list);
                    break;
            }
        }

        public void HandleHealthUpdate(IncomingEntityComponentMessage msg)
        {
            BodyPart part = (BodyPart)msg.messageParameters[1];
            int dmgCount = (int)msg.messageParameters[2];
            int maxHP = (int)msg.messageParameters[3];

            if (damageZones.Exists(x => x.location == part))
            {
                var existingZone = damageZones.First(x => x.location == part);
                existingZone.maxHealth = maxHP;

                for (int i = 0; i < dmgCount; i++)
                {
                    DamageType type = (DamageType)msg.messageParameters[4 + (i * 2)]; //Retrieve data from message in pairs starting at 4
                    int amount = (int)msg.messageParameters[5 + (i * 2)];

                    if (existingZone.damageIndex.ContainsKey(type))
                        existingZone.damageIndex[type] = amount;
                    else
                        existingZone.damageIndex.Add(type, amount);
                }

                existingZone.UpdateTotalHealth();
            }
            else
            {
                var newZone = new DamageLocation(part, maxHP, maxHP);
                damageZones.Add(newZone);

                for (int i = 0; i < dmgCount; i++)
                {
                    DamageType type = (DamageType)msg.messageParameters[4 + (i * 2)]; //Retrieve data from message in pairs starting at 4
                    int amount = (int)msg.messageParameters[5 + (i * 2)];

                    if (newZone.damageIndex.ContainsKey(type))
                        newZone.damageIndex[type] = amount;
                    else
                        newZone.damageIndex.Add(type, amount);
                }

                newZone.UpdateTotalHealth();
            }

            maxHealth = GetMaxHealth();
            health = GetHealth();
            if (health <= 0) isDead = true; //Need better logic here.
        }

        public override float GetMaxHealth()
        {
            return damageZones.Sum(x => x.maxHealth);
        }

        public override float GetHealth()
        {
            return damageZones.Sum(x => x.UpdateTotalHealth());
        }
    }
}
