using OpenTK;
using SFML.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
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
        /// Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 AABB { get; set; }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new BoundingBoxComponentState(AABB);
        }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            YamlNode node;
            if (mapping.TryGetNode("sizeX", out node))
            {
                var width = node.AsFloat() / tileSize;
                AABB = Box2.FromDimensions(AABB.Left + (AABB.Width - width) / 2f, AABB.Top, width, AABB.Height);
            }

            if (mapping.TryGetNode("sizeY", out node))
            {
                var height = node.AsFloat() / tileSize;
                AABB = Box2.FromDimensions(AABB.Left, AABB.Top + (AABB.Height - height) / 2f, AABB.Width, height);
            }

            if (mapping.TryGetNode("offsetX", out node))
            {
                var x = node.AsFloat() / tileSize;
                AABB = Box2.FromDimensions(x - AABB.Width / 2f, AABB.Top, AABB.Width, AABB.Height);
            }

            if (mapping.TryGetNode("offsetY", out node))
            {
                var y = node.AsFloat() / tileSize;
                AABB = Box2.FromDimensions(AABB.Left, y - AABB.Height / 2f, AABB.Width, AABB.Height);
            }
        }
    }
}
