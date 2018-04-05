using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;

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
        private Vector2 _linVelocity;
        private float _angVelocity;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        public float Mass
        {
            get => _mass;
            set => _mass = value;
        }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        public Vector2 LinearVelocity
        {
            get => _linVelocity;
            set => _linVelocity = value;
        }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        public float AngularVelocity
        {
            get => _angVelocity;
            set => _angVelocity = value;
        }

        public bool EdgeSlide = true;

        /// <inheritdoc />
        public override void OnAdd()
        {
            // This component requires that the entity has an AABB.
            if (!Owner.HasComponent<BoundingBoxComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PhysicsComponent)} requires {nameof(BoundingBoxComponent)}. ");

            base.OnAdd();
        }

        /// <inheritdoc />
        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mass, "mass", 1);
            serializer.DataField(ref _linVelocity, "vel", Vector2.Zero);
            serializer.DataField(ref _angVelocity, "avel", 0.0f);
            serializer.DataField(ref EdgeSlide, "edgeslide", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_mass, _linVelocity);
        }
    }
}
