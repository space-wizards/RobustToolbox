using System;
using OpenTK;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a TransformComponent.
    /// </summary>
    [Serializable]
    public class TransformComponentState : ComponentState
    {
        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public readonly Vector2 Rotation;

        /// <summary>
        ///     Constructs a new state snapshot of a TransformComponent.
        /// </summary>
        /// <param name="position">Current position offset of the entity.</param>
        /// <param name="rotation">Current direction offset of the entity.</param>
        public TransformComponentState(Vector2 position, Vector2 rotation)
            : base(NetIDs.TRANSFORM)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
