using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public partial interface IPhysicsComponent
    {
        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        float Mass { get; set; }

        /// <summary>
        ///     Current momentum of the entity in kilogram meters per second
        /// </summary>
        Vector2 Momentum { get; set; }

        /// <summary>
        ///     Apply an impulse to an entity. Velocity += impulse / InvMass.
        /// </summary>
        void ApplyImpulse(Vector2 impulse);

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

        [Obsolete("Use AnchoredChangedMessage instead")]
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
        ///     Gets all the controllers from this component.
        /// </summary>
        /// <returns>An enumerable of the controllers.</returns>
        IEnumerable<VirtualController> GetControllers();

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

        /// <summary>
        ///     Tries to sets the linear velocity of all controllers controlling
        ///     this component to zero.
        ///     This does not short-circuit on the first controller that couldn't
        ///     be stopped.
        /// </summary>
        /// <returns>True if all of the controllers were reset, false otherwise.</returns>
        bool Stop();

        /// <summary>
        /// Can this body be moved?
        /// </summary>
        /// <returns></returns>
        new bool CanMove();
    }

    partial class PhysicsComponent : IPhysicsComponent
    {
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;

        private float _angularMass = 1;
        private Vector2 _linVelocity;
        private Dictionary<Type, VirtualController> _controllers = new();

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Mass
        {
            get => _mass;
            set
            {
                if (MathHelper.CloseTo(_mass, value))
                    return;

                _mass = value;
                Dirty();
            }
        }

        private float _mass;

        /// <summary>
        /// Inverse mass of the entity in kilograms (1 / Mass).
        /// </summary>
        public float InvMass
        {
            get => CanMove() ? 1.0f / Mass : 0.0f; // Infinite mass, hopefully you didn't fuck up physics anywhere.
            set => Mass = value > 0 ? 1f / value : 0f;
        }

        /// <summary>
        /// Moment of inertia, or angular mass, in kg * m^2.
        /// </summary>
        /// <remarks>
        /// https://en.wikipedia.org/wiki/Moment_of_inertia
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float I
        {
            get => _angularMass;
            set
            {
                _angularMass = value;
                Dirty();
            }
        }

        /// <summary>
        /// Inverse moment of inertia (1 / I).
        /// </summary>
        public float InvI
        {
            get => I > 0 ? 1 / I : 0f;
            set => I = value > 0 ? 1 / value : 0f;
        }

        /// <summary>
        /// Current Force being applied to this entity in Newtons.
        /// </summary>
        /// <remarks>
        /// The force is applied to the center of mass.
        /// https://en.wikipedia.org/wiki/Force
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Force { get; set; }

        /// <summary>
        /// Current torque being applied to this entity in N*m.
        /// </summary>
        /// <remarks>
        /// The torque rotates around the Z axis on the object.
        /// https://en.wikipedia.org/wiki/Torque
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Torque { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Friction
        {
            get => _friction;
            set
            {
                if (MathHelper.CloseTo(value, _friction))
                    return;

                _friction = value;
            }
        }

        private float _friction;

        public void ApplyImpulse(Vector2 impulse)
        {
            LinearVelocity += impulse * InvMass;
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
                if (BodyType == BodyType.Static)
                    return;

                if (value != Vector2.Zero)
                    Awake = true;

                if (_linVelocity.EqualsApprox(value, 0.0001))
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
                if (BodyType == BodyType.Static)
                    return;

                if (value != 0.0f)
                    Awake = true;

                if (MathHelper.CloseTo(_angVelocity, value))
                    return;

                _angVelocity = value;
                Dirty();
            }
        }

        private float _angVelocity;

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
                if (_status == value)
                    return;

                _status = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Whether this component is on the ground
        /// </summary>
        public bool OnGround => Status == BodyStatus.OnGround &&
                                !IoCManager.Resolve<IPhysicsManager>()
                                    .IsWeightless(Owner.Transform.Coordinates);

        /// <summary>
        ///     Whether or not the entity is anchored in place.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Obsolete("Use BodyType.Static instead")]
        public bool Anchored
        {
            get => BodyType == BodyType.Static;
            set
            {
                var anchored = BodyType == BodyType.Static;

                if (anchored == value)
                    return;

                if (value)
                {
                    _bodyType = BodyType.Static;
                }
                else
                {
                    _bodyType = BodyType.Dynamic;
                }

                AnchoredChanged?.Invoke();
                SendMessage(new AnchoredChangedMessage(Anchored));
                Dirty();
            }
        }

        [Obsolete("Use AnchoredChangedMessage instead")]
        public event Action? AnchoredChanged;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Predict
        {
            get => _predict;
            set => _predict = value;
        }

        private bool _predict;

        Dictionary<Type, VirtualController> IPhysicsComponent.Controllers
        {
            get => _controllers;
            set => _controllers = value;
        }

        /// <inheritdoc />
        public T AddController<T>() where T : VirtualController, new()
        {
            if (_controllers.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"A controller of type {typeof(T)} already exists.");
            }

            var controller = _dynamicTypeFactory.CreateInstance<T>();
            controller.ControlledComponent = this;
            _controllers[typeof(T)] = controller;

            Dirty();

            return controller;
        }

        /// <inheritdoc />
        public T SetController<T>() where T : VirtualController, new()
        {
            var controller = _dynamicTypeFactory.CreateInstance<T>();
            controller.ControlledComponent = this;
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
        public IEnumerable<VirtualController> GetControllers()
        {
            return _controllers.Values;
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

        /// <summary>
        ///     Remove the proxies from all the broadphases.
        /// </summary>
        public void ClearProxies()
        {
            var broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
            var mapId = Owner.Transform.MapID;

            foreach (var fixture in Fixtures)
            {
                foreach (var (gridId, proxies) in fixture.Proxies)
                {
                    var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);
                    DebugTools.AssertNotNull(broadPhase);
                    if (broadPhase == null) continue; // TODO

                    foreach (var proxy in proxies)
                    {
                        broadPhase.RemoveProxy(proxy.ProxyId);
                    }
                }

                fixture.Proxies.Clear();
            }
        }

        public void FixtureChanged(Fixture fixture)
        {
            // TODO: Optimise this a LOT
            Dirty();
            EntitySystem.Get<SharedBroadPhaseSystem>().SynchronizeFixtures(this);
        }

        /// <summary>
        ///     Calculate our AABB without using proxies.
        /// </summary>
        /// <returns></returns>
        public Box2 GetWorldAABB()
        {
            var mapId = Owner.Transform.MapID;
            if (mapId == MapId.Nullspace)
                return new Box2();

            var worldRotation = Owner.Transform.WorldRotation;
            var bounds = new Box2();

            foreach (var fixture in Fixtures)
            {
                var aabb = fixture.Shape.CalculateLocalBounds(worldRotation);
                bounds = bounds.Union(aabb);
            }

            return bounds.Translated(Owner.Transform.WorldPosition);
        }

        /// <summary>
        ///     Get the proxies for each of our fixtures and add them to the broadphases.
        /// </summary>
        /// <param name="mapManager"></param>
        public void CreateProxies(IMapManager? mapManager = null)
        {
            DebugTools.Assert(Fixtures.Count(fix => fix.Proxies.Count != 0) == 0);

            var broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var mapId = Owner.Transform.MapID;
            var worldAABB = GetWorldAABB();
            var worldRotation = Owner.Transform.WorldRotation.Theta;

            foreach (var gridId in mapManager.FindGridIdsIntersecting(mapId, worldAABB, true))
            {
                var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);
                DebugTools.AssertNotNull(broadPhase);
                if (broadPhase == null) continue; // TODO

                Vector2 offset;
                double gridRotation = worldRotation;

                if (gridId == GridId.Invalid)
                {
                    offset = Vector2.Zero;
                }
                else
                {
                    var grid = mapManager.GetGrid(gridId);
                    offset = -grid.WorldPosition;
                    // TODO: Should probably have a helper for this
                    gridRotation = worldRotation - Owner.EntityManager.GetEntity(grid.GridEntityId).Transform.WorldRotation;
                }

                foreach (var fixture in Fixtures)
                {
                    var proxies = new FixtureProxy[1];
                    // TODO: Will need update number with ProxyCount
                    fixture.Proxies[gridId] = proxies;
                    Box2 aabb = fixture.Shape.CalculateLocalBounds(gridRotation).Translated(offset);

                    var proxy = new FixtureProxy
                    {
                        Fixture = fixture,
                        AABB =  aabb,
                        ProxyId = DynamicTree.Proxy.Free,
                    };

                    proxy.ProxyId = broadPhase.AddProxy(ref proxies[0]);
                    proxies[0] = proxy;
                }
            }
        }

        IEnumerable<IPhysBody> IPhysBody.GetCollidingEntities(Vector2 offset, bool approx)
        {
            return EntitySystem.Get<SharedBroadPhaseSystem>().GetCollidingEntities(this, offset, approx);
        }

        /// <inheritdoc />
        public bool Stop()
        {
            var successful = true;

            foreach (var controller in _controllers.Values)
            {
                successful &= controller.Stop();
            }

            return successful;
        }

        /// <inheritdoc />
        public bool CanMove()
        {
            return BodyType == BodyType.Dynamic || (!Anchored && Mass > 0);
        }
    }

    public class AnchoredChangedMessage : ComponentMessage
    {
        public readonly bool Anchored;

        public AnchoredChangedMessage(bool anchored)
        {
            Anchored = anchored;
        }
    }
}
