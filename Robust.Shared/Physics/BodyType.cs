using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     The properties of physical body. This determines how collisions will effect this body.
    /// </summary>
    [Serializable, NetSerializable]
    [Flags]
    public enum BodyType : byte
    {
        /// <summary>
        ///     Kinematic objects have to be moved manually and have their forces reset every tick.
        /// </summary>
        Kinematic = 0,

        // Custom from Box2D so we can get a good character controller and also good collisions.
        /// <summary>
        ///     Kinematic controller objects are similar to kinematic bodies except they cannot push anything at all.
        /// </summary>
        KinematicController = 1 << 1,

        /// <summary>
        ///     Static objects have infinite mass and cannot be moved by forces or collisions. They are solid,
        ///     will collide with other objects, and raise collision events. This is what you use for immovable level geometry.
        /// </summary>
        Static = 1 << 2,

        /// <summary>
        ///     Dynamic objects will respond to collisions and forces. They will raise collision events. This is what
        ///     you use for movable objects in the game.
        /// </summary>
        Dynamic = 1 << 3,
    }
}
