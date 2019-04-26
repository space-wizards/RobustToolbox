using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.Interfaces.Physics
{
    public interface ICollidable
    {
        /// <summary>
        ///     Entity that this collidable represents.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        Box2 WorldAABB { get; }

        /// <summary>
        ///     AABB of this entity in local space.
        /// </summary>
        Box2 AABB { get; }

        IPhysShape PhysicsShape { get; }

        /// <summary>
        ///     Enables or disabled collision processing of this body.
        /// </summary>
        bool CollisionEnabled { get; set; }

        /// <summary>
        ///     True if collisions should prevent movement, or just trigger bumps.
        /// </summary>
        bool IsHardCollidable { get; set; }

        /// <summary>
        ///     Bitmask of the collision layers this component is a part of.
        /// </summary>
        int CollisionLayer { get; set; }

        /// <summary>
        ///     Bitmask of the layers this component collides with.
        /// </summary>
        int CollisionMask { get; set; }

        /// <summary>
        /// Called when the collidable is bumped into by someone/something
        /// </summary>
        /// <param name="bumpedby"></param>
        void Bumped(IEntity bumpedby);

        /// <summary>
        /// Called when the collidable bumps into this entity
        /// </summary>
        /// <param name="bumpedinto"></param>
        void Bump(List<IEntity> bumpedinto);

        /// <summary>
        ///     The map index this collidable is located upon
        /// </summary>
        MapId MapID { get; }
    }
}
