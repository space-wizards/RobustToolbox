using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
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
        private Box2 _aabb;

        /// <inheritdoc />
        public override string Name => "BoundingBox";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.BOUNDING_BOX;

        /// <summary>
        /// Local Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 AABB
        {
            get => _aabb;
            set => _aabb = value;
        }

        /// <summary>
        /// World Axis Aligned Bounding Box of the entity.
        /// </summary>
        public Box2 WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                return AABB.Translated(trans.WorldPosition);
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new BoundingBoxComponentState(_aabb);
        }

        /// <inheritdoc />
        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _aabb, "aabb", new Box2(-0.5f, -0.5f, 0.5f, 0.5f));
        }
    }
}
