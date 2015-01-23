using SS14.Shared.GO;

namespace SS14.Server.GameObjects.Item.ItemCapability
{
    internal class BreatherCapability : ItemCapability
    {
        public BreatherCapability()
        {
            CapabilityType = ItemCapabilityType.Internals;
            capabilityName = "BreatherCapability";
            interactsWith = InteractsWith.Actor;
        }
    }
}