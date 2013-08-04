using SS13_Shared.GO;

namespace SGO.Item.ItemCapability
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