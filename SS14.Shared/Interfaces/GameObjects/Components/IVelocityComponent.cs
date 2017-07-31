using SFML.System;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// Stores the velocity of an entity in the world: how fast it's moving and in which direction.
    /// </summary>
    public interface IVelocityComponent : IComponent
    {
        /// <summary>
        /// The velocity of this entity (how fast it's moving and in which direction)
        /// </summary>
        Vector2f Velocity { get; set; }

        /// <summary>
        /// Horizontal speed (x axis). Equivalent to <c>Velocity.X</c>.
        /// </summary>
        float X { get; set; }

        /// <summary>
        /// Vertical speed (y axis). Equivalent to <c>Velocity.Y</c>.
        /// </summary>
        float Y { get; set; }
    }
}
