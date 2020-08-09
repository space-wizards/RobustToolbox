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


        public SharedPhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(IPhysicsComponent));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SimulateWorld(float frameTime, List<ICollidableComponent> physicsComponents)
        {
            foreach (var physics in physicsComponents)
            {
                if(!physics.CanMove())
                    continue;

                var linearVelocity = Vector2.Zero;

                foreach (var controller in physics.Controllers.Values)
                {
                    controller.UpdateBeforeProcessing();
                    linearVelocity += controller.LinearVelocity;
                }

                physics.LinearVelocity = linearVelocity;
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions(physicsComponents);

            // Remove all entities that were deleted during collision handling
            physicsComponents.RemoveAll(p => p.Deleted);

            // Process frictional forces
            foreach (var physics in physicsComponents)
            {
                ProcessFriction(physics);
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

            var multiplier = frameTime / (1f / 60);

            var divisions = Math.Clamp(
                MathF.Round(solveIterationsAt60 * multiplier, MidpointRounding.AwayFromZero),
                1,
                20
            );

            if (_timing.InSimulation) divisions = 1;

            for (var i = 0; i < divisions; i++)
            {
                foreach (var physics in physicsComponents)
                {
                    if(physics.CanMove())
                        UpdatePosition(physics, frameTime / divisions);
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
                aCollidable.SleepAccumulator++;

                if (aCollidable.LinearVelocity == Vector2.Zero)
                {
                    continue;
                }

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

        private void ProcessFriction(ICollidableComponent body)
        {
            if (body.LinearVelocity == Vector2.Zero) return;

            // Calculate frictional force
            var friction = GetFriction(body);

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, body.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = body.LinearVelocity.Normalized * -friction;

            body.LinearVelocity += frictionVelocityChange;
        }

        private static void UpdatePosition(ICollidableComponent body, float frameTime)
        {
            var ent = body.Owner;
            body.LinearVelocity = new Vector2(Math.Abs(body.LinearVelocity.X) < Epsilon ? 0.0f : body.LinearVelocity.X, Math.Abs(body.LinearVelocity.Y) < Epsilon ? 0.0f : body.LinearVelocity.Y);
            if (body.Anchored ||
                body.LinearVelocity == Vector2.Zero && Math.Abs(body.AngularVelocity) < Epsilon) return;

            if (ContainerHelpers.IsInContainer(ent) && body.LinearVelocity != Vector2.Zero)
            {
                ent.Transform.Parent!.Owner.SendMessage(ent.Transform, new RelayMovementEntityMessage(ent));
                // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                body.LinearVelocity = Vector2.Zero;
            }

            body.Owner.Transform.WorldRotation += body.AngularVelocity * frameTime;
            body.Owner.Transform.WorldPosition += body.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private bool FixClipping(List<Manifold> collisions, float divisions)
        {
            const float allowance = 1/128f;
            var percent = Math.Clamp(1f / divisions, 0.01f, 1f);
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

        private float GetFriction(ICollidableComponent body)
        {
            if (!body.OnGround)
                return 0.0f;

            var location = body.Owner.Transform;
            var grid = _mapManager.GetGrid(location.GridPosition.GridID);
            var tile = grid.GetTileRef(location.GridPosition);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
