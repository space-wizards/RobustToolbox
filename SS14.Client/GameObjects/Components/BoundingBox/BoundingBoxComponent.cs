using System;
using OpenTK;
using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    ///     to the physics system as a static (non-movable) entity.
    /// </summary>
    public class BoundingBoxComponent : ClientComponent
    {
        /// <inheritdoc />
        public override string Name => "BoundingBox";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.BOUNDING_BOX;

        /// <summary>
        ///     Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 AABB { get; private set; }

        /// <inheritdoc />
        public override Type StateType => typeof(BoundingBoxComponentState);

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            AABB = ((BoundingBoxComponentState) state).AABB;
        }
    }
}
