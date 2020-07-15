using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
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
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SimulateWorld(float frameTime, List<PhysicsComponent> physicsComponents)
        {
            foreach (var physics in physicsComponents)
            {
                physics.Controller?.UpdateBeforeProcessing();
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions();

            // Remove all entities that were deleted during collision handling
            physicsComponents.RemoveAll(p => p.Deleted);

            // Process frictional forces
            foreach (var physics in physicsComponents)
            {
                ProcessFriction(physics);
            }

            foreach (var physics in physicsComponents)
            {
                physics.Controller?.UpdateAfterProcessing();
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
        private void ProcessCollisions()
        {
            var collisionsWith = new Dictionary<ICollideBehavior, int>();

            FindCollisions();

            var counter = 0;

            while(GetNextCollision(_collisionCache, counter, out var collision))
            {
                counter++;
                var impulse = _physicsManager.SolveCollisionImpulse(collision);
                if (collision.APhysics != null)
                {
                    collision.APhysics.Momentum -= impulse;
                }

                if (collision.BPhysics != null)
                {
                    collision.BPhysics.Momentum += impulse;
                }
            }

            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                var aBehaviors = collision.A.Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in aBehaviors)
                {
                    var entity = collision.B.Owner;
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
                var bBehaviors = collision.B.Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in bBehaviors)
                {
                    var entity = collision.A.Owner;
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

        private void FindCollisions()
        {
            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            foreach (var physics in _componentManager.GetAllComponents<PhysicsComponent>())
            {
                if (physics.LinearVelocity == Vector2.Zero)
                {
                    continue;
                }

                if (!physics.Owner.TryGetComponent<CollidableComponent>(out var aCollidable))
                {
                    continue;
                }

                FindCollisionsFor(aCollidable, physics, combinations);
            }
        }

        private void FindCollisionsFor(CollidableComponent a, PhysicsComponent aPhysics,
            HashSet<(EntityUid, EntityUid)> combinations)
        {
            foreach (var b in a.GetCollidingEntities(Vector2.Zero))
            {
                var aUid = a.Owner.Uid;
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

                var bCollidable = b.GetComponent<CollidableComponent>();
                if (b.TryGetComponent<PhysicsComponent>(out var bPhysics))
                {
                    _collisionCache.Add(new Manifold(a, bCollidable, aPhysics, bPhysics, a.Hard && bCollidable.Hard, _physicsManager));
                }
                else
                {
                    _collisionCache.Add(new Manifold(a, bCollidable, aPhysics, null, a.Hard && bCollidable.Hard, _physicsManager));
                }
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

        private void ProcessFriction(PhysicsComponent physics)
        {
            if (physics.LinearVelocity == Vector2.Zero) return;

            // Calculate frictional force
            var friction = GetFriction(physics);

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, physics.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionVelocityChange;
        }

        private void UpdatePosition(PhysicsComponent physics, float frameTime)
        {
            var ent = physics.Owner;
            physics.LinearVelocity = new Vector2(Math.Abs(physics.LinearVelocity.X) < Epsilon ? 0.0f : physics.LinearVelocity.X, Math.Abs(physics.LinearVelocity.Y) < Epsilon ? 0.0f : physics.LinearVelocity.Y);
            if (physics.Anchored ||
                physics.LinearVelocity == Vector2.Zero && Math.Abs(physics.AngularVelocity) < Epsilon) return;

            if (physics.LinearVelocity != Vector2.Zero)
            {
                var entityMoveMessage = new EntityMovementMessage();
                ent.SendMessage(ent.Transform, entityMoveMessage);

                if (ContainerHelpers.IsInContainer(ent))
                {
                    var relayEntityMoveMessage = new RelayMovementEntityMessage(ent);
                    ent.Transform.Parent!.Owner.SendMessage(ent.Transform, relayEntityMoveMessage);
                    // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                    physics.LinearVelocity = Vector2.Zero;
                }
            }

            physics.Owner.Transform.WorldRotation += physics.AngularVelocity * frameTime;
            physics.Owner.Transform.WorldPosition += physics.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private bool FixClipping(List<Manifold> collisions, float divisions)
        {
            const float allowance = 0.05f;
            var percent = Math.Clamp(1f / divisions, 0.01f, 1f);
            var done = true;
            foreach (var collision in collisions)
            {
                if (!collision.Hard)
                {
                    continue;
                }

                var penetration = _physicsManager.CalculatePenetration(collision.A, collision.B);
                if (penetration > allowance)
                {
                    done = false;
                    var correction = collision.Normal * Math.Abs(penetration) * percent;
                    if (collision.APhysics != null && !collision.APhysics.Anchored && !collision.APhysics.Deleted)
                        collision.APhysics.Owner.Transform.WorldPosition -= correction;
                    if (collision.BPhysics != null && !collision.BPhysics.Anchored && !collision.BPhysics.Deleted)
                        collision.BPhysics.Owner.Transform.WorldPosition += correction;
                }
            }

            return done;
        }

        private float GetFriction(PhysicsComponent physics)
        {
            var ent = physics.Owner;
            if (ent.HasComponent<CollidableComponent>() && physics.OnGround)
            {
                var location = ent.Transform;
                var grid = _mapManager.GetGrid(location.GridPosition.GridID);
                var tile = grid.GetTileRef(location.GridPosition);
                var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }
            return 0.0f;
        }
    }
}
