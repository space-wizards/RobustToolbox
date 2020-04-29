using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    public class PhysicsComponent : SharedPhysicsComponent
    {
        private Vector2 _linVel;
        private float _angVel;
        private float _mass;
        private VirtualForce _virtualForce;

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity in kg.
        /// </summary>
        [ViewVariables]
        public override float Mass
        {
            get => _mass;
            set => _mass = value;
        }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        [ViewVariables]
        public override Vector2 LinearVelocity
        {
            get => _linVel;
            set => _linVel = value;
        }

        /// <summary>
        ///     Current angular velocity of the entity
        /// </summary>
        [ViewVariables]
        public override float AngularVelocity
        {
            get => _angVel;
            set => _angVel = value;
        }

        /// <summary>
        ///     Current momentum of the entity
        /// </summary>
        [ViewVariables]
        public override Vector2 Momentum
        {
            get => LinearVelocity * Mass;
            set => _linVel = value / Mass;
        }

        /// <summary>
        ///     Represents a virtual force acting on the physics component.
        /// </summary>
        public override VirtualForce VirtualForce
        {
            get => _virtualForce;
            set => _virtualForce = value;
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var newState = (PhysicsComponentState)curState;
            Mass = newState.Mass / 1000f; // gram to kilogram
            LinearVelocity = newState.LinearVelocity;
            AngularVelocity = newState.AngularVelocity;
        }
    }
}
