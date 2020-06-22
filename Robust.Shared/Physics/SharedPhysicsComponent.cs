using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

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
        public abstract BodyStatus Status { get; set; }

        public abstract bool OnGround { get; }

        public abstract VirtualController? Controller { get; }
        public abstract bool Anchored { get; set; }
    }
    [Serializable, NetSerializable]
    public enum BodyStatus
    {
        OnGround,
        InAir
    }
}
