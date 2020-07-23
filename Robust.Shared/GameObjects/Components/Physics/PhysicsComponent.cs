using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components
{
    [Obsolete("Use the ICollidableComponent interface, or use ICollidableComponent.Anchored if you are using this to check if the entity can be moved.")]
    public interface IPhysicsComponent : IComponent
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
        protected internal Dictionary<Type, VirtualController> Controllers { get; }

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

        T AddController<T>() where T : VirtualController, new();

        T SetController<T>() where T : VirtualController, new();

        T GetController<T>() where T : VirtualController;

        bool TryGetController<T>([NotNullWhen(true)] out T controller) where T : VirtualController;

        bool HasController<T>() where T : VirtualController;

        T EnsureController<T>() where T : VirtualController, new();

        bool EnsureController<T>(out T controller) where T : VirtualController, new();

        bool TryRemoveController<T>() where T : VirtualController;

        bool TryRemoveController<T>([NotNullWhen(true)] out T controller) where T : VirtualController;

        void RemoveControllers();
    }

    [Obsolete("Migrate to CollidableComponent")]
    [RegisterComponent]
    [ComponentReference(typeof(IPhysicsComponent))]
    public class PhysicsComponent : Component, IPhysicsComponent
    {
        private ICollidableComponent _collidableComponent = default!;
        private bool _upgradeCollidable;

        private float _mass;
        private float _angVelocity;
        private BodyStatus _status;
        private Dictionary<Type, VirtualController> _controllers = default!;
        private bool _anchored;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out CollidableComponent comp))
            {
                Logger.ErrorS("physics", $"Entity {Owner} is missing a {nameof(CollidableComponent)}, adding one for you.");
            }

            _collidableComponent = comp;

            if (_upgradeCollidable)
            {
                _collidableComponent.Mass = _mass;
                _collidableComponent.AngularVelocity = _angVelocity;
                _collidableComponent.Anchored = _anchored;
                _collidableComponent.Status = _status;
                _collidableComponent.Controllers = _controllers;
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if(serializer.Reading) // Prevents the obsolete component from writing
            {
                if(!serializer.ReadDataField("upgraded", false))
                {
                    _upgradeCollidable = true;
                    serializer.DataField<float>(ref _mass, "mass", 1);
                    serializer.DataField(ref _angVelocity, "avel", 0.0f);
                    serializer.DataField(ref _anchored, "Anchored", false);
                    serializer.DataField(ref _status, "Status", BodyStatus.OnGround);
                    serializer.DataField(ref _controllers, "Controllers", new Dictionary<Type, VirtualController>());
                }
            }
            else
            {
                var upgrade = true;
                serializer.DataField(ref upgrade, "upgraded", true);
            }
        }

        #region IPhysicsComponent Proxy

        public float Mass
        {
            get => _collidableComponent.Mass;
            set => _collidableComponent.Mass = value;
        }

        public Vector2 LinearVelocity
        {
            get => _collidableComponent.LinearVelocity;
            set => _collidableComponent.LinearVelocity = value;
        }

        public float AngularVelocity
        {
            get => _collidableComponent.AngularVelocity;
            set => _collidableComponent.AngularVelocity = value;
        }

        public Vector2 Momentum
        {
            get => _collidableComponent.Momentum;
            set => _collidableComponent.Momentum = value;
        }

        public BodyStatus Status
        {
            get => _collidableComponent.Status;
            set => _collidableComponent.Status = value;
        }

        Dictionary<Type, VirtualController> IPhysicsComponent.Controllers => _collidableComponent.Controllers;

        public bool OnGround => _collidableComponent.OnGround;

        public bool Anchored
        {
            get => _collidableComponent.Anchored;
            set => _collidableComponent.Anchored = value;
        }

        public event Action? AnchoredChanged
        {
            add => _collidableComponent.AnchoredChanged += value;
            remove => _collidableComponent.AnchoredChanged -= value;
        }

        public bool Predict
        {
            get => _collidableComponent.Predict;
            set => _collidableComponent.Predict = value;
        }

        public T AddController<T>() where T : VirtualController, new()
        {
            return _collidableComponent.AddController<T>();
        }

        public T SetController<T>() where T : VirtualController, new()
        {
            return _collidableComponent.SetController<T>();
        }

        public T GetController<T>() where T : VirtualController
        {
            return _collidableComponent.GetController<T>();
        }

        public bool TryGetController<T>([NotNullWhen(true)] out T controller) where T : VirtualController
        {
            return _collidableComponent.TryGetController(out controller);
        }

        public bool HasController<T>() where T : VirtualController
        {
            return _collidableComponent.HasController<T>();
        }

        public T EnsureController<T>() where T : VirtualController, new()
        {
            return _collidableComponent.EnsureController<T>();
        }

        public bool EnsureController<T>(out T controller) where T : VirtualController, new()
        {
            return _collidableComponent.EnsureController(out controller);
        }

        public bool TryRemoveController<T>() where T : VirtualController
        {
            return _collidableComponent.TryRemoveController<T>();
        }

        public bool TryRemoveController<T>(out T controller) where T : VirtualController
        {
            return _collidableComponent.TryRemoveController(out controller);
        }

        public void RemoveControllers()
        {
            _collidableComponent.RemoveControllers();
        }

        #endregion
    }
}
