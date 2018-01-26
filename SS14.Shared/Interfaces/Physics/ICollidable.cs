using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollidable
    {
        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        Box2 WorldAABB { get; }

        /// <summary>
        ///     AABB of this entity in local space.
        /// </summary>
        Box2 AABB { get; }

        /// <summary>
        ///     True if collisions should prevent movement, or just trigger bumps.
        /// </summary>
        bool IsHardCollidable { get; }

        /// <summary>
        ///     Called when the collidable is bumped into by someone/something
        /// </summary>
        void Bump(IEntity ent);

        /// <summary>
        ///     The map index this collidable is located upon
        /// </summary>
        MapId MapID { get; }
    }
}
