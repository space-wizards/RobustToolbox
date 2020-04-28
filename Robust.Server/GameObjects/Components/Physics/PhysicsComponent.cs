using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    public class PhysicsComponent : SharedPhysicsComponent
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
        [ViewVariables(VVAccess.ReadWrite)]
        public override float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override Vector2 LinearVelocity
        {
            get => _linVelocity;
            set
            {
                if(_linVelocity == value)
                    return;

                _linVelocity = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override float AngularVelocity
        {
            get => _angVelocity;
            set
            {
                if(_angVelocity.Equals(value))
                    return;

                _angVelocity = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override Vector2 Momentum
        {
            get => LinearVelocity * Mass;
            set => LinearVelocity = value / Mass;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool EdgeSlide { get => edgeSlide; set => edgeSlide = value; }
        private bool edgeSlide = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _anchored;
        public bool Anchored
        {
            get => _anchored;
            set
            {
                _anchored = value;
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mass, "mass", 1);
            serializer.DataField(ref _linVelocity, "vel", Vector2.Zero);
            serializer.DataField(ref _angVelocity, "avel", 0.0f);
            serializer.DataField(ref edgeSlide, "edgeslide", true);
            serializer.DataField(ref _anchored, "Anchored", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_mass, _linVelocity, _angVelocity);
        }
    }
}
