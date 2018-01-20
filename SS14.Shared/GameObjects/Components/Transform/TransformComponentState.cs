using System;
using SS14.Shared.Maths;
using SS14.Shared.Map;

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
        public readonly EntityUid? ParentID;

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        public readonly Vector2 Position;
        public readonly MapId MapID;
        public readonly GridId GridID;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public readonly Angle Rotation;

        /// <summary>
        ///     Constructs a new state snapshot of a TransformComponent.
        /// </summary>
        /// <param name="position">Current position offset of the entity.</param>
        /// <param name="rotation">Current direction offset of the entity.</param>
        /// <param name="parentID">Current parent transform of this entity.</param>
        public TransformComponentState(LocalCoordinates position, Angle rotation, EntityUid? parentID)
            : base(NetIDs.TRANSFORM)
        {
            Position = position.Position;
            MapID = position.MapID;
            GridID = position.GridID;
            Rotation = rotation;
            ParentID = parentID;
        }
    }
}
