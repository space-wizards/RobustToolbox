using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Item.ItemCapability
{
    public class ToolCapability : ItemCapability
    {
        public ToolCapability()
        {
            CapabilityType = SS13_Shared.GO.ItemCapabilityType.Tool;
            verbs.Add(0, SS13_Shared.GO.ItemCapabilityVerb.Hit);
            capabilityName = "ToolCapability";
        }
    }
}
