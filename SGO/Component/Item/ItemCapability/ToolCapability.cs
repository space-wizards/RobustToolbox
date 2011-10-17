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
            CapabilityType = SS3D_shared.GO.ItemCapabilityType.Tool;
            verbs.Add(0, SS3D_shared.GO.ItemCapabilityVerb.Hit);
            capabilityName = "ToolCapability";
        }
    }
}
