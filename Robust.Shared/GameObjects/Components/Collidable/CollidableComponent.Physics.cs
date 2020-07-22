using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public partial interface ICollidableComponent
    {
        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        float Mass { get; set; }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        Vector2 LinearVelocity { get; set; }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        float AngularVelocity { get; set; }

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        Vector2 Momentum { get; set; }

        /// <summary>
        ///     The current status of the object
        /// </summary>
        BodyStatus Status { get; set; }

        /// <summary>
        ///     Represents a virtual controller acting on the physics component.
        /// </summary>
        VirtualController? Controller { get; set; }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        bool OnGround { get; }

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        bool Anchored { get; set; }

        event Action? AnchoredChanged;

        bool Predict { get; set; }
        void SetController<T>() where T : VirtualController, new();
        void RemoveController();
    }

    partial class CollidableComponent : ICollidableComponent
    {
        private float _mass;
        private Vector2 _linVelocity;
        private float _angVelocity;
        private VirtualController? _controller;
        private bool _anchored;

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
                if (_linVelocity == value)
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
                if (_angVelocity.Equals(value))
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
            set => _controller = value;
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

        public event Action? AnchoredChanged;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Predict { get; set; }

        [Obsolete("This only exists for legacy reasons.")]
        public bool CanMove([NotNullWhen(true)]IPhysicsComponent physics)
        {
            return Owner.TryGetComponent(out physics) && !Anchored;
        }

        public void SetController<T>() where T : VirtualController, new()
        {
            _controller = new T { ControlledComponent = this };
            Dirty();
        }

        public void RemoveController()
        {
            if (_controller != null)
            {
                _controller.ControlledComponent = null;
                _controller = null;
            }
        }
    }
}
