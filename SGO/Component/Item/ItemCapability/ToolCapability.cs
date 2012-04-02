using SS13_Shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class ToolCapability : ItemCapability
    {
        public ToolCapability()
        {
            CapabilityType = ItemCapabilityType.Tool;
            verbs.Add(0, ItemCapabilityVerb.Hit);
            capabilityName = "ToolCapability";
        }
    }
}