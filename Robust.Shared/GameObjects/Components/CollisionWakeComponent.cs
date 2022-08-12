using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An optimisation component for stuff that should be set as collidable when it's awake and non-collidable when asleep.
    /// </summary>
    [NetworkedComponent()]
    [Access(typeof(CollisionWakeSystem))]
    public sealed class CollisionWakeComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = true;

        [Serializable, NetSerializable]
        public sealed class CollisionWakeState : ComponentState
        {
            public bool Enabled { get; }

            public CollisionWakeState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
