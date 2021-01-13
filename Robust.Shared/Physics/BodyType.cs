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
        ///     Will not be processed by the collision system. Basically "out of phase" with the world.
        ///     They will not raise collision events. Forces are still applied to the body, and it can move.
        /// </summary>
        None,

        /// <summary>
        ///     Static objects have infinite mass and cannot be moved by forces or collisions. They are solid,
        ///     will collide with other objects, and raise collision events. This is what you use for immovable level geometry.
        /// </summary>
        Static,

        /// <summary>
        ///     Dynamic objects will respond to collisions and forces. They will raise collision events. This is what
        ///     you use for movable objects in the game.
        /// </summary>
        Dynamic,

        /// <summary>
        ///     Trigger objects cannot be moved by collisions or forces. They are not solid and won't block objects.
        ///     Collision events will still be raised.
        /// </summary>
        Trigger,
    }
}
