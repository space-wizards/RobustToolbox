using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPhysBody
    {
        /// <summary>
        ///     Entity that this physBody represents.
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

        List<IPhysShape> PhysicsShapes { get; }

        /// <summary>
        /// Enables or disabled collision processing of this body.
        /// </summary>
        bool CollisionEnabled { get; set; }

        /// <summary>
        /// True if collisions should prevent movement, or just trigger bumps.
        /// </summary>
        bool IsHardCollidable { get; set; }

        /// <summary>
        /// Bitmask of the collision layers this body is a part of. The layers are calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionLayer { get; }

        /// <summary>
        /// Bitmask of the layers this body collides with. The mask is calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionMask { get; }

        /// <summary>
        /// Called when the physBody is bumped into by someone/something
        /// </summary>
        /// <param name="bumpedby"></param>
        void Bumped(IEntity bumpedby);

        /// <summary>
        /// Called when the physBody bumps into this entity
        /// </summary>
        /// <param name="bumpedinto"></param>
        void Bump(List<IEntity> bumpedinto);

        /// <summary>
        ///     The map index this physBody is located upon
        /// </summary>
        MapId MapID { get; }
    }
}
