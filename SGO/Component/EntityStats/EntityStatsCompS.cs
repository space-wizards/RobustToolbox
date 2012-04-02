using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;
using System.Linq;
using System.Text;
using System.Reflection;
using System;
using System.Xml.Linq;
using System.Xml;

namespace SGO
{
    public class EntityStatsComp : GameObjectComponent
    {
        Dictionary<DamageType, int> armorStats = new Dictionary<DamageType, int>();

        public EntityStatsComp()
            : base()
        {
            family = ComponentFamily.EntityStats;

            foreach(var dmgType in Enum.GetValues(typeof(DamageType)))
            {
                if (!armorStats.Keys.Contains((DamageType)dmgType))
                    armorStats.Add((DamageType)dmgType, 0);
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetArmorValues:
                    var dmgType = (DamageType)list[0];
                    return new ComponentReplyMessage(ComponentMessageType.ReturnArmorValues, GetArmorValue(dmgType));

                default:
                    return base.RecieveMessage(sender, type, list);
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (ComponentMessageType)message.messageParameters[0];
            switch (type)
            {
                case (ComponentMessageType.GetArmorValues): //Add message for sending complete listing.
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client, ComponentMessageType.ReturnArmorValues, (int)((DamageType)message.messageParameters[1]), GetArmorValue((DamageType)message.messageParameters[1]));
                    break;

                default:
                    base.HandleNetworkMessage(message, client);
                    break;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void HandleExtendedParameters(System.Xml.Linq.XElement extendedParameters)
        {
            foreach (XElement entityStat in extendedParameters.Descendants("EntityArmor"))
            {
                DamageType type = (DamageType)Enum.Parse(typeof(DamageType), entityStat.Attribute("type").Value, true); //Add check for parsing. Handle exceptions if invalid name.
                int value = int.Parse(entityStat.Attribute("value").Value); //See comment above.

                armorStats[type] = value;
            }
        }

        public void SetArmorValue(DamageType damType, int value)
        {
            if (armorStats.ContainsKey(damType))
                armorStats[damType] = value;
            else
                armorStats.Add(damType, value);
        }

        public int GetArmorValue(DamageType damType)
        {
            int armorVal = 0;

            EquipmentComponent eqComp = (EquipmentComponent)Owner.GetComponent(ComponentFamily.Equipment);
            if (eqComp != null)
            {
                foreach (Entity ent in eqComp.equippedEntities.Values)
                {
                    EntityStatsComp entStatComp = (EntityStatsComp)ent.GetComponent(ComponentFamily.EntityStats);
                    if (entStatComp != null)
                        armorVal += entStatComp.GetArmorValue(damType);
                }
            }

            if (armorStats.ContainsKey(damType))
                armorVal += armorStats[damType];

            return armorVal;
        }
    }
}
