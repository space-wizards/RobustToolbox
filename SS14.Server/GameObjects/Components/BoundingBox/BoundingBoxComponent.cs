using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    /// to the physics system as a static (non-movable) entity.
    /// </summary>
    public class BoundingBoxComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "BoundingBox";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.BOUNDING_BOX;

        /// <summary>
        /// Local Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 AABB { get; set; } = new Box2(-0.5f, -0.5f, 0.5f, 0.5f);

        /// <summary>
        /// World Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                var bounds = AABB;

                return bounds.Translated(trans.WorldPosition);
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new BoundingBoxComponentState(AABB);
        }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {

            YamlNode node;
            if (mapping.TryGetNode("sizeX", out node))
            {
                var width = node.AsFloat();
                AABB = Box2.FromDimensions(AABB.Left + (AABB.Width - width) / 2f, AABB.Top, width, AABB.Height);
            }

            if (mapping.TryGetNode("sizeY", out node))
            {
                var height = node.AsFloat();
                AABB = Box2.FromDimensions(AABB.Left, AABB.Top + (AABB.Height - height) / 2f, AABB.Width, height);
            }

            if (mapping.TryGetNode("offsetX", out node))
            {
                var x = node.AsFloat();
                AABB = Box2.FromDimensions(x - AABB.Width / 2f, AABB.Top, AABB.Width, AABB.Height);
            }

            if (mapping.TryGetNode("offsetY", out node))
            {
                var y = node.AsFloat();
                AABB = Box2.FromDimensions(AABB.Left, y - AABB.Height / 2f, AABB.Width, AABB.Height);
            }
        }
    }
}
