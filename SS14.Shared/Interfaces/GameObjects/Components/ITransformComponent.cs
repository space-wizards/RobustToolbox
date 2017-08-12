using System;
using OpenTK;
using SS14.Shared.Maths;

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
        Angle Rotation { get; }

        /// <summary>
        ///     Event that gets invoked every time the position gets modified through properties such as <see cref="Rotation" />.
        /// </summary>
        event EventHandler<VectorEventArgs> OnMove;
    }
}
