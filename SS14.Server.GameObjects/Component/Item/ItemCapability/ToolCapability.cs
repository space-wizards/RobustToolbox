using SS14.Shared.GO;

namespace SS14.Server.GameObjects.Item.ItemCapability
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