using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An optimisation component for stuff that should be set as collidable when its awake and non-collidable when asleep.
    /// </summary>
    [RegisterComponent]
    public sealed class CollisionWakeComponent : Component
    {
        public override string Name => "CollisionWake";

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PhysicsWakeCompMessage msg:
                    msg.Body.CanCollide = true;
                    break;
                case PhysicsSleepCompMessage msg:
                    msg.Body.CanCollide = false;
                    break;
            }
        }
    }
}
