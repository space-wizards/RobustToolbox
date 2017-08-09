using System;
using OpenTK;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public interface ITransformComponent : IComponent
    {
        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        Vector2 Rotation { get; }

        /// <summary>
        ///     Event that gets invoked every time the position gets modified through properties such as <see cref="Rotation" />.
        /// </summary>
        event EventHandler<VectorEventArgs> OnMove;
    }
}
