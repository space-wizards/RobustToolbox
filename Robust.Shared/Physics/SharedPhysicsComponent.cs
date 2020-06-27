using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    public abstract class SharedPhysicsComponent: Component
    {
        /// <inheritdoc />
        public override string Name => "Physics";

        [ViewVariables]
        public abstract Vector2 LinearVelocity { get; set; }
        [ViewVariables]
        public abstract float AngularVelocity { get; set; }
        [ViewVariables]
        public abstract float Mass { get; set; }
        [ViewVariables]
        public abstract Vector2 Momentum { get; set; }
        [ViewVariables]
        public abstract BodyStatus Status { get; set; }

        [ViewVariables]
        public abstract bool OnGround { get; }

        [ViewVariables]
        public abstract VirtualController? Controller { get; }
        [ViewVariables]
        public abstract bool Anchored { get; set; }
    }
    [Serializable, NetSerializable]
    public enum BodyStatus
    {
        OnGround,
        InAir
    }
}
