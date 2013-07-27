using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;
using System;
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;
using Lidgren;
using Lidgren.Network;
using SS13_Shared.GO.Component.EntityStats;

namespace CGO
{
    public class EntityStatsComp : GameObjectComponent
    {
        public EntityStatsComp():base()
        {
            Family = ComponentFamily.EntityStats;
        }
        public override Type StateType
        {
            get { return typeof (EntityStatsComponentState); }
        }

        Dictionary<DamageType, int> armorStats = new Dictionary<DamageType, int>();

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.ReturnArmorValues):
                    if (!armorStats.Keys.Contains((DamageType)message.MessageParameters[1]))
                        armorStats.Add((DamageType)message.MessageParameters[1], (int)message.MessageParameters[2]);
                    else
                        armorStats[(DamageType)message.MessageParameters[1]] = (int)message.MessageParameters[2];
                    break;

                default:
                    base.HandleNetworkMessage(message);
                    break;
            }
        }

        public void PullFullUpdate() //Add proper message for this.
        {
            foreach (var curr in Enum.GetValues(typeof(DamageType))) 
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.GetArmorValues, (int)curr);
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
