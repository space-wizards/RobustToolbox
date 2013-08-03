using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.Network;
using ServerInterfaces.Player;

namespace SGO.Item.ItemCapability
{
    class BreatherCapability : ItemCapability
    {        
        public BreatherCapability()
        {
            CapabilityType = ItemCapabilityType.Internals;
            capabilityName = "BreatherCapability";
            interactsWith = InteractsWith.Actor;
        }
    }
}
