using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.EntityStats;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class EntityStatsComp : Component
    {
        public override string Name => "EntityStats";
        private readonly Dictionary<DamageType, int> armorStats = new Dictionary<DamageType, int>();

        public EntityStatsComp()
        {
            Family = ComponentFamily.EntityStats;

            foreach (object dmgType in Enum.GetValues(typeof(DamageType)))
            {
                if (!armorStats.Keys.Contains((DamageType)dmgType))
                    armorStats.Add((DamageType)dmgType, 0);
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
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
            var type = (ComponentMessageType)message.MessageParameters[0];
            switch (type)
            {
                case (ComponentMessageType.GetArmorValues): //Add message for sending complete listing.
                    //Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client, ComponentMessageType.ReturnArmorValues, (int)((DamageType)message.MessageParameters[1]), GetArmorValue((DamageType)message.MessageParameters[1]));
                    break;

                default:
                    base.HandleNetworkMessage(message, client);
                    break;
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("armor", out node))
            {
                foreach (KeyValuePair<YamlNode, YamlNode> stat in (YamlMappingNode)node)
                {
                    var type = stat.Key.AsEnum<DamageType>();
                    //Add check for parsing. Handle exceptions if invalid name.
                    int value = stat.Value.AsInt(); //See comment above.

                    armorStats[type] = value;
                }
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

            var eqComp = (EquipmentComponent)Owner.GetComponent(ComponentFamily.Equipment);
            if (eqComp != null)
            {
                foreach (IEntity ent in eqComp.equippedEntities.Values)
                {
                    var entStatComp = (EntityStatsComp)ent.GetComponent(ComponentFamily.EntityStats);
                    if (entStatComp != null)
                        armorVal += entStatComp.GetArmorValue(damType);
                }
            }

            if (armorStats.ContainsKey(damType))
                armorVal += armorStats[damType];

            return armorVal;
        }

        public override ComponentState GetComponentState()
        {
            return new EntityStatsComponentState(armorStats);
        }
    }
}
