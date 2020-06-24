using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
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
        private BodyStatus _status;
        private VirtualController? _controller;

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity in kg.
        /// </summary>
        [ViewVariables]
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
        ///     Current velocity of the entity.
        /// </summary>
        [ViewVariables]
        public override Vector2 LinearVelocity
        {
            get => _linVel;
            set
            {
                _linVel = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current angular velocity of the entity
        /// </summary>
        [ViewVariables]
        public override float AngularVelocity
        {
            get => _angVel;
            set
            {
                _angVel = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current momentum of the entity
        /// </summary>
        [ViewVariables]
        public override Vector2 Momentum
        {
            get => LinearVelocity * Mass;
            set
            {
                _linVel = value / Mass;
                Dirty();
            }
        }

        /// <summary>
        ///     The current status of the object
        /// </summary>
        public override BodyStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)] public bool Predict { get; set; }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        public override bool OnGround => Status == BodyStatus.OnGround &&
                                         !IoCManager.Resolve<IPhysicsManager>()
                                             .IsWeightless(Owner.Transform.GridPosition);

        /// <summary>
        ///     Represents a virtual controller acting on the physics component.
        /// </summary>
        public override VirtualController? Controller => _controller;

        public void SetController<T>() where T: VirtualController, new()
        {
            _controller = new T {ControlledComponent = this};
            Dirty();
        }

        [ViewVariables]
        public override bool Anchored { get; set; }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var newState = (PhysicsComponentState)curState;
            Mass = newState.Mass / 1000f; // gram to kilogram

            LinearVelocity = newState.LinearVelocity;
            // Logger.Debug($"{IGameTiming.TickStampStatic}: [{Owner}] {LinearVelocity}");
            AngularVelocity = newState.AngularVelocity;
            // TODO: Does it make sense to reset controllers here?
            // This caused space movement to break in content and I'm not 100% sure this is a good fix.
            // Look man the CM test is in 5 hours cut me some slack.
            //_controller = null;
        }
    }
}
