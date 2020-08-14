using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
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
        [Dependency] private readonly IComponentManager _componentManager = default!;

        private const float Epsilon = 1.0e-6f;

        private readonly List<Manifold> _collisionCache = new List<Manifold>();
        private readonly HashSet<ICollidableComponent> _awakeBodies = new HashSet<ICollidableComponent>();

        public SharedPhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(IPhysicsComponent));
        }

        protected void SimulateWorld(float deltaTime, List<ICollidableComponent> physicsComponents, bool prediction)
        {
            _awakeBodies.Clear();

            foreach (var body in physicsComponents)
            {
                if(prediction && !body.Predict)
                    continue;

                if(!body.Awake)
                    continue;

                _awakeBodies.Add(body);

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
                const float damping = 0.98f; // space/air friction, so everything eventually stops moving
                body.LinearVelocity += body.Force * body.InvMass * deltaTime * damping;
                body.AngularVelocity += body.Torque * body.InvI * deltaTime * damping;

                // forces are instantaneous, so these properties are cleared
                // once integrated. If you want to apply a continuous force,
                // it has to be re-applied every tick.
                body.Force = Vector2.Zero;
                body.Torque = 0f;
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions(physicsComponents);

            // Remove all entities that were deleted during collision handling
            physicsComponents.RemoveAll(p => p.Deleted);

            // Process frictional forces
            foreach (var physics in physicsComponents)
            {
                ProcessFriction(physics, deltaTime);
            }

            foreach (var physics in physicsComponents)
            {
                foreach (var controller in physics.Controllers.Values)
                {
                    controller.UpdateAfterProcessing();
                }
            }

            // Remove all entities that were deleted due to the controller
            physicsComponents.RemoveAll(p => p.Deleted);

            const int solveIterationsAt60 = 4;

            var multiplier = deltaTime / (1f / 60);

            var divisions = FloatMath.Clamp(
                MathF.Round(solveIterationsAt60 * multiplier, MidpointRounding.AwayFromZero),
                1,
                20
            );

            if (_timing.InSimulation) divisions = 1;

            for (var i = 0; i < divisions; i++)
            {
                foreach (var physics in physicsComponents)
                {
                    if(physics.Awake && physics.CanMove())
                        UpdatePosition(physics, deltaTime / divisions);
                }

                for (var j = 0; j < divisions; ++j)
                {
                    if (FixClipping(_collisionCache, divisions))
                    {
                        break;
                    }
                }
            }
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions(IEnumerable<ICollidableComponent> bodies)
        {
            var collisionsWith = new Dictionary<ICollideBehavior, int>();

            FindCollisions(bodies);

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

        private void FindCollisions(IEnumerable<ICollidableComponent> bodies)
        {
            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            foreach (var aCollidable in bodies)
            {
                if(!aCollidable.Awake)
                    continue;

                FindCollisionsFor(aCollidable, combinations);
            }
        }

        private void FindCollisionsFor(ICollidableComponent a, HashSet<(EntityUid, EntityUid)> combinations)
        {
            foreach (var b in _physicsManager.GetCollidingEntities(a, Vector2.Zero))
            {
                var aUid = a.Entity.Uid;
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
                _collisionCache.Add(new Manifold(a, bCollidable, a.Hard && bCollidable.Hard));
            }
        }

        private bool GetNextCollision(List<Manifold> collisions, int counter, out Manifold collision)
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

        private static void UpdatePosition(IPhysBody body, float frameTime)
        {
            var ent = body.Entity;

            if (!body.CanMove() || (body.LinearVelocity.LengthSquared < Epsilon && MathF.Abs(body.AngularVelocity) < Epsilon))
                return;

            if (ContainerHelpers.IsInContainer(ent) && body.LinearVelocity != Vector2.Zero)
            {
                ent.Transform.Parent!.Owner.SendMessage(ent.Transform, new RelayMovementEntityMessage(ent));
                // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                body.LinearVelocity = Vector2.Zero;
            }

            body.WorldRotation += body.AngularVelocity * frameTime;
            body.WorldPosition += body.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private bool FixClipping(List<Manifold> collisions, float divisions)
        {
            const float allowance = 1 / 128f;
            var percent = FloatMath.Clamp(1f / divisions, 0.01f, 1f);
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
                    collision.A.Owner.Transform.WorldPosition -= correction;
                if (collision.B.CanMove())
                    collision.B.Owner.Transform.WorldPosition += correction;
            }

            return done;
        }

        private (float friction, float gravity) GetFriction(ICollidableComponent body)
        {
            if (!body.OnGround)
                return (0f, 0f);

            var location = body.Owner.Transform;
            var grid = _mapManager.GetGrid(location.GridPosition.GridID);
            var tile = grid.GetTileRef(location.GridPosition);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return (tileDef.Friction, grid.HasGravity ? 9.8f : 0f);
        }
    }
}
