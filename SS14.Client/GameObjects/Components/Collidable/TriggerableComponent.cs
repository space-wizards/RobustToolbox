using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects
{
    public class TriggerableComponent : CollidableComponent
    {
        public override string Name => "Triggerable";
        public override uint? NetID => NetIDs.TRIGGERABLE;
        public TriggerableComponent()
        {
            isHardCollidable = false;
        }
    }
}
