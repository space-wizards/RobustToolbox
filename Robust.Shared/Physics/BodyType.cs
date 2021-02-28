using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     The properties of physical body. This determines how collisions will effect this body.
    /// </summary>
    [Serializable, NetSerializable]
    public enum BodyType : byte
    {
        /// <summary>
        ///     Kinematic objects have to be moved manually and have their forces reset every tick.
        /// </summary>
        Kinematic = 0,

        /// <summary>
        ///     Static objects have infinite mass and cannot be moved by forces or collisions. They are solid,
        ///     will collide with other objects, and raise collision events. This is what you use for immovable level geometry.
        /// </summary>
        Static = 1 << 0,

        /// <summary>
        ///     Dynamic objects will respond to collisions and forces. They will raise collision events. This is what
        ///     you use for movable objects in the game.
        /// </summary>
        Dynamic = 1 << 1,
    }
}
