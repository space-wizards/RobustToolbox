using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
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
        ///     Collidable objects that are awake and usable for world simulation.
        /// </summary>
        private readonly HashSet<ICollidableComponent> _awakeBodies = new HashSet<ICollidableComponent>();

        /// <summary>
        ///     Collidable objects that are awake and predicted and usable for world simulation.
        /// </summary>
        private readonly HashSet<ICollidableComponent> _predictedAwakeBodies = new HashSet<ICollidableComponent>();

        /// <summary>
        ///     VirtualControllers on applicable ICollidableComponents
        /// </summary>
        private Dictionary<ICollidableComponent, IEnumerable<VirtualController>> _controllers = 
            new Dictionary<ICollidableComponent, IEnumerable<VirtualController>>();
        
        // We'll defer changes to ICollidable until each step is done.
        private readonly List<ICollidableComponent> _queuedDeletions = new List<ICollidableComponent>();
        private readonly List<ICollidableComponent> _queuedUpdates = new List<ICollidableComponent>();
        
        /// <summary>
        ///     Updates to EntityTree etc. that are deferred until the end of physics.
        /// </summary>
        private readonly HashSet<ICollidableComponent> _deferredUpdates = new HashSet<ICollidableComponent>();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollidableUpdateMessage>(HandleCollidableUpdateMessage);
        }

        private void HandleCollidableUpdateMessage(CollidableUpdateMessage message)
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
        ///     Process the changes to cached ICollidables
        /// </summary>
        private void ProcessQueue()
        {
            // At this stage only the dynamictree cares about asleep bodies
            
            foreach (var collidable in _queuedUpdates)
            {
                if (collidable.Awake)
                {
                    if (collidable.Predict)
                        _predictedAwakeBodies.Add(collidable);
                    
                    _awakeBodies.Add(collidable);
                    
                }

                if (collidable.Controllers.Count > 0 && !_controllers.ContainsKey(collidable))
                    _controllers.Add(collidable, collidable.Controllers.Values);

            }
            
            _queuedUpdates.Clear();

            foreach (var collidable in _queuedDeletions)
            {
                _awakeBodies.Remove(collidable);
                _predictedAwakeBodies.Remove(collidable);
                _controllers.Remove(collidable);
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

                var linearVelocity = Vector2.Zero;

                foreach (var controller in body.Controllers.Values)
                {
                    controller.UpdateBeforeProcessing();
                    linearVelocity += controller.LinearVelocity;
                }

                // i'm not sure if this is the proper way to solve this, but
                // these are not kinematic bodies, so we need to preserve the previous
                // velocity.
                //if (body.LinearVelocity.LengthSquared < linearVelocity.LengthSquared)
                    body.LinearVelocity = linearVelocity;

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
            
            // Remove all entities that were deleted during collision handling
            ProcessQueue();

            // Process frictional forces
            foreach (var physics in _awakeBodies)
            {
                ProcessFriction(physics, deltaTime);
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

            const int solveIterationsAt60 = 4;

            var multiplier = deltaTime / (1f / 60);

            var divisions = MathHelper.Clamp(
                MathF.Round(solveIterationsAt60 * multiplier, MidpointRounding.AwayFromZero),
                1,
                20
            );

            if (_timing.InSimulation) divisions = 1;

            for (var i = 0; i < divisions; i++)
            {
                foreach (var collidable in simulatedBodies)
                {
                    if(collidable.CanMove())
                        UpdatePosition(collidable, deltaTime / divisions);
                }

                for (var j = 0; j < divisions; ++j)
                {
                    if (FixClipping(_collisionCache, divisions))
                    {
                        break;
                    }
                }
            }

            // As we also defer the updates for the _collisionCache we need to update all entities
            foreach (var collidable in _deferredUpdates)
            {
                var transform = collidable.Owner.Transform;
                transform.DeferUpdates = false;
                transform.RunCollidableDeferred();
            }
            
            _deferredUpdates.Clear();
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions(IEnumerable<ICollidableComponent> awakeBodies)
        {
            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            foreach (var aCollidable in awakeBodies)
            {
                foreach (var b in _physicsManager.GetCollidingEntities(aCollidable, Vector2.Zero))
                {
                    var aUid = aCollidable.Entity.Uid;
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

                    var bCollidable = b.GetComponent<ICollidableComponent>();
                    _collisionCache.Add(new Manifold(aCollidable, bCollidable, aCollidable.Hard && bCollidable.Hard));
                }
            }

            var counter = 0;
            while(GetNextCollision(_collisionCache, counter, out var collision))
            {
                collision.A.WakeBody();
                collision.B.WakeBody();

                counter++;
                var impulse = _physicsManager.SolveCollisionImpulse(collision);
                if (collision.A.CanMove())
                {
                    collision.A.Momentum -= impulse;
                }

                if (collision.B.CanMove())
                {
                    collision.B.Momentum += impulse;
                }
            }

            var collisionsWith = new Dictionary<ICollideBehavior, int>();
            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                var aBehaviors = collision.A.Entity.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in aBehaviors)
                {
                    var entity = collision.B.Entity;
                    if (entity.Deleted) continue;
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
                var bBehaviors = collision.B.Entity.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in bBehaviors)
                {
                    var entity = collision.A.Entity;
                    if (entity.Deleted) continue;
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

        private bool GetNextCollision(IReadOnlyList<Manifold> collisions, int counter, out Manifold collision)
        {
            // The *4 is completely arbitrary
            if (counter > collisions.Count * 4)
            {
                collision = default;
                return false;
            }
            var indexes = new List<int>();
            for (int i = 0; i < collisions.Count; i++)
            {
                indexes.Add(i);
            }
            _random.Shuffle(indexes);
            foreach (var index in indexes)
            {
                if (collisions[index].Unresolved)
                {
                    collision = collisions[index];
                    return true;
                }
            }

            collision = default;
            return false;
        }

        private void ProcessFriction(ICollidableComponent body, float deltaTime)
        {
            if (body.LinearVelocity == Vector2.Zero) return;

            // sliding friction coefficient, and current gravity at current location
            var (friction, gravity) = GetFriction(body);

            // friction between the two objects
            var effectiveFriction = friction * body.Friction;

            // current acceleration due to friction
            var fAcceleration = effectiveFriction * gravity;

            // integrate acceleration
            var fVelocity = fAcceleration * deltaTime;

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(fVelocity, body.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = body.LinearVelocity.Normalized * -friction;

            body.LinearVelocity += frictionVelocityChange;
        }

        private void UpdatePosition(ICollidableComponent collidable, float frameTime)
        {
            var ent = collidable.Entity;

            if (!collidable.CanMove() || (collidable.LinearVelocity.LengthSquared < Epsilon && MathF.Abs(collidable.AngularVelocity) < Epsilon))
                return;

            if (collidable.LinearVelocity != Vector2.Zero)
            {
                if (ContainerHelpers.IsInContainer(ent))
                {
                    var relayEntityMoveMessage = new RelayMovementEntityMessage(ent);
                    ent.Transform.Parent!.Owner.SendMessage(ent.Transform, relayEntityMoveMessage);
                    // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                    collidable.LinearVelocity = Vector2.Zero;
                }
            }
            
            collidable.Owner.Transform.DeferUpdates = true;
            _deferredUpdates.Add(collidable);
            collidable.WorldRotation += collidable.AngularVelocity * frameTime;
            collidable.WorldPosition += collidable.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private bool FixClipping(List<Manifold> collisions, float divisions)
        {
            const float allowance = 1 / 128f;
            var percent = MathHelper.Clamp(1f / divisions, 0.01f, 1f);
            var done = true;
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
                var correction = collision.Normal * Math.Abs(penetration) * percent;
                if (collision.A.CanMove())
                {
                    collision.A.Owner.Transform.DeferUpdates = true;
                    _deferredUpdates.Add(collision.A);
                    collision.A.Owner.Transform.WorldPosition -= correction;
                }

                if (collision.B.CanMove())
                {
                    collision.B.Owner.Transform.DeferUpdates = true;
                    _deferredUpdates.Add(collision.B);
                    collision.B.Owner.Transform.WorldPosition += correction;
                }
            }

            return done;
        }

        private (float friction, float gravity) GetFriction(ICollidableComponent body)
        {
            if (!body.OnGround)
                return (0f, 0f);

            var location = body.Owner.Transform;
            var grid = _mapManager.GetGrid(location.Coordinates.GetGridId(EntityManager));
            var tile = grid.GetTileRef(location.Coordinates);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return (tileDef.Friction, grid.HasGravity ? 9.8f : 0f);
        }
    }
}
