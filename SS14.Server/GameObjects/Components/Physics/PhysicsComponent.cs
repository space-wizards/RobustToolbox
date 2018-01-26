using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    public class PhysicsComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity.
        /// </summary>
        public float Mass { get; set; }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        public Vector2 Velocity { get; set; }

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<BoundingBoxComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PhysicsComponent)} requires {nameof(BoundingBoxComponent)}. ");

            base.OnAdd(owner);
        }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("mass", out node))
                Mass = node.AsFloat();
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(Mass, Velocity);
        }
    }
}
