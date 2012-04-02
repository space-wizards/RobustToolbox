using System.Collections.Generic;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using System.Drawing;
using System;
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;

namespace CGO
{
    public class EntityStatsComp : GameObjectComponent
    {
        public override ComponentFamily Family { get { return ComponentFamily.EntityStats; } }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.AddStatusEffect):
                    break;

                case (ComponentMessageType.RemoveStatusEffect):
                    break;

                default:
                    base.HandleNetworkMessage(message);
                    break;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }
    }
}
