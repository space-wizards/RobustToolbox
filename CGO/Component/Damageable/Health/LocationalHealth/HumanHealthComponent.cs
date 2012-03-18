using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using ClientInterfaces.Player;
using SS13.IoC;
using SS13_Shared.GO;
using SS13_Shared;

namespace CGO
{
    public class HumanHealthComponent : HealthComponent //Behaves like health component but tracks damage of individual zones.
    {                                                   //Useful for mobs.

        public List<DamageLocation> DamageZones = new List<DamageLocation>(); //makes this protected again.

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    HandleHealthUpdate(message);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentLocationHealth:
                    var location = (BodyPart)list[0];
                    if (DamageZones.Exists(x => x.Location == location))
                    {
                        var dmgLoc = DamageZones.First(x => x.Location == location);
                        reply = new ComponentReplyMessage(ComponentMessageType.CurrentLocationHealth, location, dmgLoc.UpdateTotalHealth(), dmgLoc.MaxHealth);
                    }
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(), GetMaxHealth());
                    break;
            }

            return reply;
        }

        public void HandleHealthUpdate(IncomingEntityComponentMessage msg)
        {
            var part = (BodyPart)msg.MessageParameters[1];
            var dmgCount = (int)msg.MessageParameters[2];
            var maxHP = (int)msg.MessageParameters[3];

            if (DamageZones.Exists(x => x.Location == part))
            {
                var existingZone = DamageZones.First(x => x.Location == part);
                existingZone.MaxHealth = maxHP;

                for (var i = 0; i < dmgCount; i++)
                {
                    var type = (DamageType)msg.MessageParameters[4 + (i * 2)]; //Retrieve data from message in pairs starting at 4
                    var amount = (int)msg.MessageParameters[5 + (i * 2)];

                    if (existingZone.DamageIndex.ContainsKey(type))
                        existingZone.DamageIndex[type] = amount;
                    else
                        existingZone.DamageIndex.Add(type, amount);
                }

                existingZone.UpdateTotalHealth();
            }
            else
            {
                var newZone = new DamageLocation(part, maxHP, maxHP);
                DamageZones.Add(newZone);

                for (var i = 0; i < dmgCount; i++)
                {
                    var type = (DamageType)msg.MessageParameters[4 + (i * 2)]; //Retrieve data from message in pairs starting at 4
                    var amount = (int)msg.MessageParameters[5 + (i * 2)];

                    if (newZone.DamageIndex.ContainsKey(type))
                        newZone.DamageIndex[type] = amount;
                    else
                        newZone.DamageIndex.Add(type, amount);
                }

                newZone.UpdateTotalHealth();
            }

            MaxHealth = GetMaxHealth();
            Health = GetHealth();
            if (Health <= 0) IsDead = true; //Need better logic here.

            if(IoCManager.Resolve<IPlayerManager>().ControlledEntity.Uid == Owner.Uid)
                IoCManager.Resolve<IUserInterfaceManager>().ComponentUpdate(GuiComponentType.StatPanelComponent);
        }

        public override float GetMaxHealth()
        {
            return DamageZones.Sum(x => x.MaxHealth);
        }

        public override float GetHealth()
        {
            return DamageZones.Sum(x => x.UpdateTotalHealth());
        }
    }
}
