using System;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a BoundingBoxComponent.
    /// </summary>
    [Serializable]
    public class BoundingBoxComponentState : ComponentState
    {
        /// <summary>
        ///     Current AABB of the entity.
        /// </summary>
        public readonly Box2 AABB;

        /// <summary>
        ///     Constructs a new state snapshot of a PhysicsComponent.
        /// </summary>
        /// <param name="aabb">Current AABB of the entity.</param>
        public BoundingBoxComponentState(Box2 aabb)
            : base(NetIDs.BOUNDING_BOX)
        {
            AABB = aabb;
        }
    }
}
