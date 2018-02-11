using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
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
        private float _mass;
        private Vector2 _velocity;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity.
        /// </summary>
        public float Mass
        {
            get => _mass;
            set => _mass = value;
        }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        public Vector2 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<BoundingBoxComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PhysicsComponent)} requires {nameof(BoundingBoxComponent)}. ");

            base.OnAdd(owner);
        }

        /// <inheritdoc />
        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mass, "mass", 1);
            serializer.DataField(ref _velocity, "vel", Vector2.Zero);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_mass, _velocity);
        }
    }
}
