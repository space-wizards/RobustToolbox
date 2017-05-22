using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.EntityStats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class EntityStatsComp : Component
    {
        private Dictionary<DamageType, int> armorStats = new Dictionary<DamageType, int>();

        public EntityStatsComp()
        {
            Family = ComponentFamily.EntityStats;
        }

        public override Type StateType
        {
            get { return typeof (EntityStatsComponentState); }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.ReturnArmorValues):
                    if (!armorStats.Keys.Contains((DamageType) message.MessageParameters[1]))
                        armorStats.Add((DamageType) message.MessageParameters[1], (int) message.MessageParameters[2]);
                    else
                        armorStats[(DamageType) message.MessageParameters[1]] = (int) message.MessageParameters[2];
                    break;

                default:
                    base.HandleNetworkMessage(message, sender);
                    break;
            }
        }

        public void PullFullUpdate() //Add proper message for this.
        {
            foreach (object curr in Enum.GetValues(typeof (DamageType)))
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered,
                                                  ComponentMessageType.GetArmorValues, (int) curr);
        }

        public int GetArmorValue(DamageType damType)
        {
            if (armorStats.ContainsKey(damType)) return armorStats[damType];
            else return 0;
        }

        public override void HandleComponentState(dynamic state)
        {
            armorStats = state.ArmorStats;
        }
    }
}