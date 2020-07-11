using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    [RegisterComponent]
    public class PhysicsComponent: Component, IComponent
    {
        private float _mass;
        private Vector2 _linVelocity;
        private float _angVelocity;
        private VirtualController? _controller;
        private BodyStatus _status;
        private bool _anchored;

        public Action? AnchoredChanged;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Mass
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
        public Vector2 LinearVelocity
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
        public float AngularVelocity
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
        public Vector2 Momentum
        {
            get => LinearVelocity * Mass;
            set => LinearVelocity = value / Mass;
        }

        /// <summary>
        ///     The current status of the object
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public BodyStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Represents a virtual controller acting on the physics component.
        /// </summary>
        public VirtualController? Controller
        {
            get => _controller;
        }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        public bool OnGround => Status == BodyStatus.OnGround &&
                                !IoCManager.Resolve<IPhysicsManager>()
                                    .IsWeightless(Owner.Transform.GridPosition);

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Anchored
        {
            get => _anchored;
            set
            {
                _anchored = value;
                AnchoredChanged?.Invoke();
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Predict { get; set; }

        public void SetController<T>() where T: VirtualController, new()
        {
            _controller = new T {ControlledComponent = this};
            Dirty();
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField<float>(ref _mass, "mass", 1);
            serializer.DataField(ref _linVelocity, "vel", Vector2.Zero);
            serializer.DataField(ref _angVelocity, "avel", 0.0f);
            serializer.DataField(ref _anchored, "Anchored", false);
            serializer.DataField(ref _status, "Status", BodyStatus.OnGround);
            serializer.DataField(ref _controller, "Controller", null);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_mass, LinearVelocity, AngularVelocity, Anchored);
        }

        public void RemoveController()
        {
            if (_controller != null)
            {
                _controller.ControlledComponent = null;
                _controller = null;
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            RemoveController();
        }

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
            Anchored = newState.Anchored;
            // TODO: Does it make sense to reset controllers here?
            // This caused space movement to break in content and I'm not 100% sure this is a good fix.
            // Look man the CM test is in 5 hours cut me some slack.
            //_controller = null;
            // Reset predict flag to false to avoid predicting stuff too long.
            // Another possibly bad hack for content at the moment.
            Predict = false;
        }
    }

    [Serializable, NetSerializable]
    public enum BodyStatus
    {
        OnGround,
        InAir
    }
}
