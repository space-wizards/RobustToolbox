using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public abstract class SharedPhysicsComponent: Component
    {
        /// <inheritdoc />
        public override string Name => "Physics";

        public abstract Vector2 LinearVelocity { get; set; }
        public abstract float AngularVelocity { get; set; }
        public abstract float Mass { get; set; }
        public abstract Vector2 Momentum { get; set; }
        public abstract VirtualForce VirtualForce { get; set; }
    }
}
