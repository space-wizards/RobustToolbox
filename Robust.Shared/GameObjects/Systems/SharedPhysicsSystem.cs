using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private const float Epsilon = 1.0e-6f;

        private readonly List<Manifold> _collisionCache = new List<Manifold>();

        /// <summary>
        ///     Physics objects that are awake and usable for world simulation.
        /// </summary>
        private readonly HashSet<IPhysicsComponent> _awakeBodies = new HashSet<IPhysicsComponent>();

        /// <summary>
        ///     Physics objects that are awake and predicted and usable for world simulation.
        /// </summary>
        private readonly HashSet<IPhysicsComponent> _predictedAwakeBodies = new HashSet<IPhysicsComponent>();

        /// <summary>
        ///     VirtualControllers on applicable <see cref="IPhysicsComponent"/>s
        /// </summary>
        private Dictionary<IPhysicsComponent, IEnumerable<VirtualController>> _controllers =
            new Dictionary<IPhysicsComponent, IEnumerable<VirtualController>>();

        // We'll defer changes to IPhysicsComponent until each step is done.
        private readonly List<IPhysicsComponent> _queuedDeletions = new List<IPhysicsComponent>();
        private readonly List<IPhysicsComponent> _queuedUpdates = new List<IPhysicsComponent>();

        /// <summary>
        ///     Updates to EntityTree etc. that are deferred until the end of physics.
        /// </summary>
        private readonly HashSet<IPhysicsComponent> _deferredUpdates = new HashSet<IPhysicsComponent>();

        // CVars aren't replicated to client (yet) so not using a cvar server-side for this.
        private float _speedLimit = 30.0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PhysicsUpdateMessage>(HandlePhysicsUpdateMessage);
        }

        private void HandlePhysicsUpdateMessage(PhysicsUpdateMessage message)
        {
            if (message.Component.Deleted || !message.Component.Awake)
            {
                _queuedDeletions.Add(message.Component);
            }
            else
            {
                _queuedUpdates.Add(message.Component);
            }
        }

        /// <summary>
        ///     Process the changes to cached <see cref="IPhysicsComponent"/>s
        /// </summary>
        private void ProcessQueue()
        {
            // At this stage only the dynamictree cares about asleep bodies
            // Implicitly awake bodies so don't need to check .Awake again
            // Controllers should wake their body up (inside)
            foreach (var physics in _queuedUpdates)
            {
                if (physics.Predict)
                    _predictedAwakeBodies.Add(physics);

                _awakeBodies.Add(physics);

                if (physics.Controllers.Count > 0 && !_controllers.ContainsKey(physics))
                    _controllers.Add(physics, physics.Controllers.Values);

            }

            _queuedUpdates.Clear();

            foreach (var physics in _queuedDeletions)
            {
                _awakeBodies.Remove(physics);
                _predictedAwakeBodies.Remove(physics);
                _controllers.Remove(physics);
            }

            _queuedDeletions.Clear();
        }

        /// <summary>
        ///     Simulates the physical world for a given amount of time.
        /// </summary>
        /// <param name="deltaTime">Delta Time in seconds of how long to simulate the world.</param>
        /// <param name="prediction">Should only predicted entities be considered in this simulation step?</param>
        protected void SimulateWorld(float deltaTime, bool prediction)
        {
            var simulatedBodies = prediction ? _predictedAwakeBodies : _awakeBodies;

            ProcessQueue();

            foreach (var body in simulatedBodies)
            {
                // running prediction updates will not cause a body to go to sleep.
                if(!prediction)
                    body.SleepAccumulator++;

                // if the body cannot move, nothing to do here
                if(!body.CanMove())
                    continue;

                var linearVelocity = body.WarmStart ? body.LinearVelocity : Vector2.Zero;

                // TODO: For whatever reason if you use deltaTime here instead of worldposition you get massive misprediction
                foreach (var controller in body.Controllers.Values)
                {
                    controller.UpdateBeforeProcessing();
                    linearVelocity += controller.LinearVelocity * deltaTime;
                    linearVelocity += controller.Impulse * body.InvMass * deltaTime;
                    controller.Impulse = Vector2.Zero;
                }

                if (linearVelocity != Vector2.Zero && linearVelocity.LengthSquared < 0.00001f)
                {
                    body.LinearVelocity = Vector2.Zero;
                }
                else
                {
                    body.LinearVelocity = linearVelocity;
                }
                // Integrate forces
                body.LinearVelocity += body.Force * body.InvMass * deltaTime;
                body.AngularVelocity += body.Torque * body.InvI * deltaTime;

                // forces are instantaneous, so these properties are cleared
                // once integrated. If you want to apply a continuous force,
                // it has to be re-applied every tick.
                body.Force = Vector2.Zero;
                body.Torque = 0f;
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions(_awakeBodies);

            if (_collisionCache.Count > 0)
            {
                VelocitySolver();
            }

            CollisionBehaviors();

            // Remove all entities that were deleted during collision handling
            ProcessQueue();

            // Process frictional forces
            foreach (var physics in _awakeBodies)
            {
                ProcessFriction(physics);
            }

            foreach (var (_, controllers) in _controllers)
            {
                foreach (var controller in controllers)
                {
                    controller.UpdateAfterProcessing();
                }
            }

            // Remove all entities that were deleted due to the controller
            ProcessQueue();

            PositionSolver(_collisionCache);

            foreach (var physics in simulatedBodies)
            {
                if (physics.LinearVelocity != Vector2.Zero)
                {
                    UpdatePosition(physics);
                }
            }

            // As we also defer the updates for the _collisionCache we need to update all entities
            foreach (var physics in _deferredUpdates)
            {
                var transform = physics.Owner.Transform;
                transform.DeferUpdates = false;
                transform.RunPhysicsDeferred();
            }

            _deferredUpdates.Clear();
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions(IEnumerable<IPhysicsComponent> awakeBodies)
        {
            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            foreach (var aPhysics in awakeBodies)
            {
                foreach (var b in _physicsManager.GetCollidingEntities(aPhysics, aPhysics.LinearVelocity, false))
                {
                    var aUid = aPhysics.Entity.Uid;
                    var bUid = b.Uid;

                    if (bUid.CompareTo(aUid) > 0)
                    {
                        var tmpUid = bUid;
                        bUid = aUid;
                        aUid = tmpUid;
                    }

                    if (!combinations.Add((aUid, bUid)))
                    {
                        continue;
                    }

                    var bPhysics = b.GetComponent<IPhysicsComponent>();
                    _collisionCache.Add(new Manifold(aPhysics, bPhysics, aPhysics.Hard && bPhysics.Hard));
                }
            }
        }

        private void CollisionBehaviors()
        {
            var collisionsWith = new Dictionary<ICollideBehavior, int>();
            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                foreach (var behavior in collision.A.Entity.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    var entity = collision.B.Entity;
                    if (entity.Deleted) break;
                    behavior.CollideWith(entity);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }

                foreach (var behavior in collision.B.Entity.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    var entity = collision.A.Entity;
                    if (entity.Deleted) break;
                    behavior.CollideWith(entity);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }
            }

            foreach (var behavior in collisionsWith.Keys)
            {
                behavior.PostCollide(collisionsWith[behavior]);
            }
        }

        private void VelocitySolver(byte iterations = 4)
        {
            for (var i = 0; i < iterations; i++)
            {
                var offset = _random.Next(_collisionCache.Count - 1);
                for (var j = 0; j < _collisionCache.Count; j++)
                {
                    var collision = _collisionCache[(j + offset) % _collisionCache.Count];
                    if (!collision.Unresolved) continue;

                    collision.A.WakeBody();
                    collision.B.WakeBody();

                    var impulse = _physicsManager.SolveCollisionImpulse(collision);
                    if (collision.A.CanMove())
                    {
                        collision.A.ApplyImpulse(-impulse);
                    }

                    if (collision.B.CanMove())
                    {
                        collision.B.ApplyImpulse(impulse);
                    }
                }
            }
        }

        // Based off of Randy Gaul's ImpulseEngine code
        // https://github.com/RandyGaul/ImpulseEngine/blob/5181fee1648acc4a889b9beec8e13cbe7dac9288/Manifold.cpp#L123a
        private void PositionSolver(List<Manifold> collisions, byte iterations = 1)
        {
            const float allowance = 1 / 256.0f;
            var percent = MathHelper.Clamp(0.4f / iterations, 0.01f, 1f);
            var done = true;

            for (var i = 0; i < iterations; i++)
            {
                foreach (var collision in collisions)
                {
                    if (!collision.Hard)
                    {
                        continue;
                    }

                    var penetration = _physicsManager.CalculatePenetration(collision.A, collision.B);

                    if (penetration <= allowance)
                        continue;

                    done = false;
                    //var correction = collision.Normal * Math.Abs(penetration) * percent;
                    var correction = collision.Normal * Math.Max(penetration - allowance, 0.0f) / (collision.A.InvMass + collision.B.InvMass) * percent;
                    if (collision.A.CanMove())
                    {
                        collision.A.Owner.Transform.DeferUpdates = true;
                        _deferredUpdates.Add(collision.A);
                        collision.A.Owner.Transform.WorldPosition -= correction * collision.A.InvMass;
                    }

                    if (collision.B.CanMove())
                    {
                        collision.B.Owner.Transform.DeferUpdates = true;
                        _deferredUpdates.Add(collision.B);
                        collision.B.Owner.Transform.WorldPosition += correction * collision.B.InvMass;
                    }
                }
            }
        }

        /// <summary>
        ///     Process friction between tiles and entities. Does not process collision friction.
        /// </summary>
        /// <param name="body"></param>
        private void ProcessFriction(IPhysicsComponent body)
        {
            if (body.LinearVelocity == Vector2.Zero || body.Status == BodyStatus.InAir) return;

            var friction = GetFriction(body);

            // friction between the two objects - Static friction not modelled
            var dynamicFriction = MathF.Sqrt(friction * body.Friction) * body.LinearVelocity.Length / 9.8f;

            if (dynamicFriction == 0.0f)
                return;

            var frictionVelocityChange = body.LinearVelocity.Normalized * -dynamicFriction;

            body.LinearVelocity += frictionVelocityChange;
        }

        private void UpdatePosition(IPhysicsComponent physics)
        {
            var ent = physics.Entity;

            if ((physics.LinearVelocity.LengthSquared < Epsilon && MathF.Abs(physics.AngularVelocity) < Epsilon))
                return;

            if (physics.LinearVelocity != Vector2.Zero)
            {
                if (ContainerHelpers.IsInContainer(ent))
                {
                    var relayEntityMoveMessage = new RelayMovementEntityMessage(ent);
                    ent.Transform.Parent!.Owner.SendMessage(ent.Transform, relayEntityMoveMessage);
                    // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                    physics.LinearVelocity = Vector2.Zero;
                }
            }

            physics.Owner.Transform.DeferUpdates = true;
            _deferredUpdates.Add(physics);

            var newPosition = physics.WorldPosition + physics.LinearVelocity;
            var owner = physics.Owner;
            var transform = owner.Transform;

            // Change parent if necessary
            if (!ContainerHelpers.IsInContainer(owner))
            {
                // This shoouullddnnn'''tt de-parent anything in a container because none of that should have physics applied to it.
                if (_mapManager.TryFindGridAt(transform.MapID, newPosition, out var grid) &&
                    grid.GridEntityId.IsValid() &&
                    grid.GridEntityId != owner.Uid)
                {
                    if (grid.GridEntityId != transform.ParentUid)
                        transform.AttachParent(owner.EntityManager.GetEntity(grid.GridEntityId));
                }
                else
                {
                    transform.AttachParent(_mapManager.GetMapEntity(transform.MapID));
                }
            }

            physics.WorldRotation += physics.AngularVelocity;
            physics.WorldPosition = newPosition;
        }

        private float GetFriction(IPhysicsComponent body)
        {
            var transform = body.Owner.Transform;

            if (body.Status != BodyStatus.OnGround || transform.GridID == GridId.Invalid)
                return 0f;

            var grid = _mapManager.GetGrid(transform.Coordinates.GetGridId(EntityManager));
            var tile = grid.GetTileRef(transform.Coordinates);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
