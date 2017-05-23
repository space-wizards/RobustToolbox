using SS14.Shared.IoC;
using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    [Component("Triggerable")]
    public class TriggerableComponent : CollidableComponent
    {
        public TriggerableComponent()
        {
            isHardCollidable = false;
        }
    }
}
