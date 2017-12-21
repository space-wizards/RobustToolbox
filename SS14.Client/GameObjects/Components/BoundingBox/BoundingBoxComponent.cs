using System;
using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    ///     to the physics system as a static (non-movable) entity.
    /// </summary>
    public class BoundingBoxComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "BoundingBox";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.BOUNDING_BOX;

        /// <summary>
        ///     Local Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 AABB { get; private set; }

        /// <summary>
        ///     World Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                var bounds = AABB;

                return Box2.FromDimensions(
                    bounds.Left + trans.WorldPosition.X,
                    bounds.Top + trans.WorldPosition.Y,
                    bounds.Width,
                    bounds.Height);
            }
        }

        /// <inheritdoc />
        public override Type StateType => typeof(BoundingBoxComponentState);

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            AABB = ((BoundingBoxComponentState)state).AABB;
        }
    }
}
