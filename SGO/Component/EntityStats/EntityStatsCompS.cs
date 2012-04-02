using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class EntityStatsComp : GameObjectComponent
    {
        private readonly Dictionary<DamageType, int> armorStats = new Dictionary<DamageType, int>();

        public EntityStatsComp()
        {
            family = ComponentFamily.EntityStats;

            foreach (object dmgType in Enum.GetValues(typeof (DamageType)))
            {
                if (!armorStats.Keys.Contains((DamageType) dmgType))
                    armorStats.Add((DamageType) dmgType, 0);
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetArmorValues:
                    var dmgType = (DamageType) list[0];
                    return new ComponentReplyMessage(ComponentMessageType.ReturnArmorValues, armorStats[dmgType]);

                default:
                    return base.RecieveMessage(sender, type, list);
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];
            switch (type)
            {
                case (ComponentMessageType.GetArmorValues): //Add message for sending complete listing.
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client,
                                                      ComponentMessageType.ReturnArmorValues,
                                                      armorStats[(DamageType) message.MessageParameters[1]]);
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

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement entityStat in extendedParameters.Descendants("EntityArmor"))
            {
                var type = (DamageType) Enum.Parse(typeof (DamageType), entityStat.Attribute("type").Value, true);
                //Add check for parsing. Handle exceptions if invalid name.
                int value = int.Parse(entityStat.Attribute("value").Value); //See comment above.

                armorStats[type] = value;
            }
        }
    }
}