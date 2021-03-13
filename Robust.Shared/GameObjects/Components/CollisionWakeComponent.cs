using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An optimisation component for stuff that should be set as collidable when it's awake and non-collidable when asleep.
    /// </summary>
    public sealed class CollisionWakeComponent : Component
    {
        public override string Name => "CollisionWake";

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent(out IPhysBody? body))
            {
                body.CanCollide = true;
            }
        }
    }
}
