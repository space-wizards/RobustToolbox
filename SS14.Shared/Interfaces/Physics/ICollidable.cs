using SFML.Graphics;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.Interfaces.Physics
{
    public interface ICollidable
    {
        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        FloatRect WorldAABB { get; }

        /// <summary>
        ///     AABB of this entity in local space.
        /// </summary>
        FloatRect AABB { get; }

        /// <summary>
        ///     True if collisions should prevent movement, or just trigger bumps.
        /// </summary>
        bool IsHardCollidable { get; }

        /// <summary>
        ///     Called when the collidable is bumped into by someone/something
        /// </summary>
        void Bump(IEntity ent);
    }
}
