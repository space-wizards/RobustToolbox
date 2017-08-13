using System;
using OpenTK;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a TransformComponent.
    /// </summary>
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public readonly IEntity Parent;

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public readonly Angle Rotation;

        /// <summary>
        ///     Constructs a new state snapshot of a TransformComponent.
        /// </summary>
        /// <param name="position">Current position offset of the entity.</param>
        /// <param name="rotation">Current direction offset of the entity.</param>
        /// <param name="parent">Current parent entity of this entity.</param>
        public TransformComponentState(Vector2 position, Angle rotation, IEntity parent)
            : base(NetIDs.TRANSFORM)
        {
            Position = position;
            Rotation = rotation;
            Parent = parent;
        }
    }
}
