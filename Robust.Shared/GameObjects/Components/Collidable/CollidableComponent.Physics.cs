using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        ///     Whether this component is on the ground
        /// </summary>
        bool OnGround { get; }

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        bool Anchored { get; set; }

        event Action? AnchoredChanged;

        bool Predict { get; set; }

        protected internal Dictionary<Type, VirtualController> Controllers { get; set; }

        /// <summary>
        ///     Adds a controller of type <see cref="T"/> to this component, throwing
        ///     an error if one already exists.
        /// </summary>
        /// <typeparam name="T">The controller type to add.</typeparam>
        /// <returns>The newly added controller.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Throws <see cref="InvalidOperationException"/> if a controller of type
        ///     <see cref="T"/> already exists.
        /// </exception>
        T AddController<T>() where T : VirtualController, new();

        /// <summary>
        ///     Adds a controller of type <see cref="T"/> to this component.
        /// </summary>
        /// <typeparam name="T">The controller type to add.</typeparam>
        /// <returns>The newly added controller.</returns>
        T SetController<T>() where T : VirtualController, new();

        /// <summary>
        ///     Gets a controller of type <see cref="T"/> from this component.
        /// </summary>
        /// <typeparam name="T">The controller type to get.</typeparam>
        /// <returns>The existing controller.</returns>
        /// <exception cref="KeyNotFoundException">
        ///     Throws <see cref="KeyNotFoundException"/> if no controller exists with
        ///     type <see cref="T"/>.
        /// </exception>
        T GetController<T>() where T : VirtualController;

        /// <summary>
        ///     Tries to get a controller of type <see cref="T"/> from this component.
        /// </summary>
        /// <param name="controller">The controller if found or null otherwise.</param>
        /// <typeparam name="T">The type of the controller to find.</typeparam>
        /// <returns>True if the controller was found, false otherwise.</returns>
        bool TryGetController<T>([NotNullWhen(true)] out T controller) where T : VirtualController;

        /// <summary>
        ///     Checks if this component has a controller of type <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the controller to check.</typeparam>
        /// <returns>True if the controller exists, false otherwise.</returns>
        bool HasController<T>() where T : VirtualController;

        /// <summary>
        ///     Convenience wrapper to implement "create controller if it does
        ///     not already exist".
        ///     Always gives you back a controller, and creates it if it does
        ///     not exist yet.
        /// </summary>
        /// <typeparam name="T">The type of the controller to fetch or create.</typeparam>
        /// <returns>
        ///     The existing controller, or the new controller if none existed yet.
        /// </returns>
        T EnsureController<T>() where T : VirtualController, new();

        /// <summary>
        ///     Convenience wrapper to implement "create controller if it does
        ///     not already exist".
        ///     Always gives you back a controller, and creates it if it does
        ///     not exist yet.
        /// </summary>
        /// <param name="controller">
        ///     The existing controller, or the new controller if none existed yet.
        /// </param>
        /// <typeparam name="T">The type of the controller to fetch or create.</typeparam>
        /// <returns>
        ///     True if the component already existed, false if it had to be created.
        /// </returns>
        bool EnsureController<T>(out T controller) where T : VirtualController, new();

        /// <summary>
        ///     Removes the controller of type <see cref="T"/> if one exists.
        /// </summary>
        /// <typeparam name="T">The type of the controller to remove</typeparam>
        /// <returns>True if the component was removed, false otherwise.</returns>
        bool TryRemoveController<T>() where T : VirtualController;

        /// <summary>
        ///     Removes the controller of type <see cref="T"/> if one exists,
        ///     and if so returns it.
        /// </summary>
        /// <param name="controller">
        ///     The controller if one was removed, null otherwise.
        /// </param>
        /// <typeparam name="T">The type of the controller to remove</typeparam>
        /// <returns>True if the component was removed, false otherwise.</returns>
        bool TryRemoveController<T>([NotNullWhen(true)] out T controller) where T : VirtualController;

        /// <summary>
        ///     Removes all controllers from this component.
        /// </summary>
        void RemoveControllers();
    }

    partial class CollidableComponent : ICollidableComponent
    {
        private float _mass;
        private Vector2 _linVelocity;
        private float _angVelocity;
        private Dictionary<Type, VirtualController> _controllers = new Dictionary<Type, VirtualController>();
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

        Dictionary<Type, VirtualController> ICollidableComponent.Controllers
        {
            get => _controllers;
            set => _controllers = value;
        }

        [Obsolete("This only exists for legacy reasons.")]
        public bool CanMove([NotNullWhen(true)]IPhysicsComponent physics)
        {
            return Owner.TryGetComponent(out physics) && !Anchored;
        }

        /// <inheritdoc />
        public T AddController<T>() where T : VirtualController, new()
        {
            if (_controllers.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"A controller of type {typeof(T)} already exists.");
            }

            var controller = new T {ControlledComponent = this};
            _controllers[typeof(T)] = controller;

            Dirty();

            return controller;
        }

        /// <inheritdoc />
        public T SetController<T>() where T : VirtualController, new()
        {
            var controller = new T {ControlledComponent = this};
            _controllers[typeof(T)] = controller;

            Dirty();

            return controller;
        }

        /// <inheritdoc />
        public T GetController<T>() where T : VirtualController
        {
            return (T) _controllers[typeof(T)];
        }

        /// <inheritdoc />
        public bool TryGetController<T>([NotNullWhen(true)] out T controller) where T : VirtualController
        {
            controller = null!;

            var found = _controllers.TryGetValue(typeof(T), out var value);

            return found && (controller = (value as T)!) != null;
        }

        /// <inheritdoc />
        public bool HasController<T>() where T : VirtualController
        {
            return _controllers.ContainsKey(typeof(T));
        }

        /// <inheritdoc />
        public T EnsureController<T>() where T : VirtualController, new()
        {
            if (TryGetController(out T controller))
            {
                return controller;
            }

            controller = AddController<T>();

            return controller;
        }

        /// <inheritdoc />
        public bool EnsureController<T>(out T controller) where T : VirtualController, new()
        {
            if (TryGetController(out controller))
            {
                return true;
            }

            controller = AddController<T>();
            return false;
        }

        /// <inheritdoc />
        public bool TryRemoveController<T>() where T : VirtualController
        {
            var removed = _controllers.Remove(typeof(T), out var controller);

            if (controller != null)
            {
                controller.ControlledComponent = null;
            }

            Dirty();

            return removed;
        }

        /// <inheritdoc />
        public bool TryRemoveController<T>([NotNullWhen(true)] out T controller) where T : VirtualController
        {
            controller = null!;
            var removed = _controllers.Remove(typeof(T), out var virtualController);

            if (virtualController != null)
            {
                controller = (T) virtualController;
                controller.ControlledComponent = null;
            }

            Dirty();

            return removed;
        }

        /// <inheritdoc />
        public void RemoveControllers()
        {
            foreach (var controller in _controllers.Values)
            {
                controller.ControlledComponent = null;
            }

            _controllers.Clear();
            Dirty();
        }
    }
}
