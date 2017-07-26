using SFML.System;
using System;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// Stores the position of an entity in the world. These are global coordinates, not related to grids or alike.
    /// </summary>
    public interface ITransformComponent : IComponent
    {
        /// <summary>
        /// The absolute world position of the entity owning this component.
        /// </summary>
        Vector2f Position { get; set; }

        /// <summary>
        /// Moves the component with a certain offset, instead of setting direct coordinates.
        /// </summary>
        /// <param name="offset"></param>
        void Offset(Vector2f offset);

        /// <summary>
        /// X coordinate. Equivalent to <c>Position.X</c>.
        /// </summary>
        float X { get; }

        /// <summary>
        /// Y coordinate. Equivalent to <c>Position.Y</c>.
        /// </summary>
        float Y { get; }

        /// <summary>
        /// Event that gets invoked every time the position gets modified through properties such as <see cref="Position" />.
        /// </summary>
        event EventHandler<VectorEventArgs> OnMove;
    }
}
