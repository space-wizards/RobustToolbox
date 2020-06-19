using System;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Log;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class PhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const float Epsilon = 1.0e-6f;

        private readonly List<Manifold> _collisionCache = new List<Manifold>();

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, RelevantEntities.Where(e => !e.Deleted && !_pauseManager.IsEntityPaused(e)).ToList());
        }

        private void SimulateWorld(float frameTime, ICollection<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                var physics = entity.GetComponent<PhysicsComponent>();

                physics.Controller?.UpdateBeforeProcessing();
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions();

            // Remove all entities that were deleted during collision handling
            foreach (var entity in entities.Where(e => e.Deleted).ToList())
            {
                entities.Remove(entity);
            }

            // Process frictional forces
            foreach (var entity in entities)
            {
                ProcessFriction(entity, frameTime);
            }

            foreach (var entity in entities)
            {
                var physics = entity.GetComponent<PhysicsComponent>();

                physics.Controller?.UpdateAfterProcessing();
            }

            // Remove all entities that were deleted due to the controller
            foreach (var entity in entities.Where(e => e.Deleted).ToList())
            {
                entities.Remove(entity);
            }

            const int solveIterationsAt60 = 4;

            var multiplier = frameTime / (1f / 60);

            var divisions = Math.Clamp(
                MathF.Round(solveIterationsAt60 * multiplier, MidpointRounding.AwayFromZero),
                1,
                60
            );

            for (var i = 0; i < divisions; i++)
            {
                foreach (var entity in entities)
                {
                    UpdatePosition(entity, frameTime / divisions);
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
            _collisionCache.Clear();
            var collisionsWith = new Dictionary<ICollideBehavior, int>();
            var physicsComponents = new Dictionary<ICollidableComponent, PhysicsComponent>();

            foreach (var collision in FindCollisions(RelevantEntities, physicsComponents))
            {
                _collisionCache.Add(collision);
            }

            var counter = 0;

            while(GetNextCollision(_collisionCache, counter, out var collision))
            {
                counter++;
                var impulse = _physicsManager.SolveCollisionImpulse(collision);
                if (physicsComponents.ContainsKey(collision.A))
                {
                    physicsComponents[collision.A].Momentum -= impulse;
                }

                if (physicsComponents.ContainsKey(collision.B))
                {
                    physicsComponents[collision.B].Momentum += impulse;
                }
            }

            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                var aBehaviors = ((CollidableComponent)collision.A).Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in aBehaviors)
                {
                    var entity = ((CollidableComponent)collision.B).Owner;
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
                var bBehaviors = ((CollidableComponent)collision.B).Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in bBehaviors)
                {
                    var entity = ((CollidableComponent)collision.A).Owner;
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

        private IEnumerable<Manifold> FindCollisions(IEnumerable<IEntity> entities, Dictionary<ICollidableComponent, PhysicsComponent> physicsComponents)
        {
            var combinations = new List<(EntityUid, EntityUid)>();
            foreach (var entity in entities)
            {
                if (entity.Deleted)
                {
                    continue;
                }

                if (entity.GetComponent<PhysicsComponent>().LinearVelocity == Vector2.Zero)
                {
                    continue;
                }

                if (!entity.TryGetComponent<CollidableComponent>(out var a)) continue;

                foreach (var collision in FindCollisionsFor(a, combinations, physicsComponents))
                {
                    yield return collision;
                }
            }
        }

        private IEnumerable<Manifold> FindCollisionsFor(CollidableComponent a,
            List<(EntityUid, EntityUid)> combinations,
            Dictionary<ICollidableComponent, PhysicsComponent> physicsComponents)
        {
            foreach (var b in a.GetCollidingEntities(Vector2.Zero).Select(e => e.GetComponent<CollidableComponent>()))
            {
                if (combinations.Contains((a.Owner.Uid, b.Owner.Uid)) ||
                    combinations.Contains((b.Owner.Uid, a.Owner.Uid)))
                {
                    continue;
                }

                combinations.Add((a.Owner.Uid, b.Owner.Uid));
                var aPhysics = a.Owner.GetComponent<PhysicsComponent>();
                physicsComponents[a] = aPhysics;
                if (b.Owner.TryGetComponent<PhysicsComponent>(out var bPhysics))
                {
                    physicsComponents[b] = bPhysics;
                    yield return new Manifold(a, b, aPhysics, bPhysics);
                }
                else
                {
                    yield return new Manifold(a, b, aPhysics, null);
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

        private void ProcessFriction(IEntity entity, float frameTime)
        {
            // A constant that scales frictional force to work with the rest of the engine
            const float frictionScalingConstant = 60.0f;

            var physics = entity.GetComponent<PhysicsComponent>();

            if (physics.LinearVelocity == Vector2.Zero) return;

            // Calculate frictional force
            var friction = GetFriction(entity) * frameTime * frictionScalingConstant;

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, physics.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionVelocityChange;
        }

        private void UpdatePosition(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();
            physics.LinearVelocity = new Vector2(Math.Abs(physics.LinearVelocity.X) < Epsilon ? 0.0f : physics.LinearVelocity.X, Math.Abs(physics.LinearVelocity.Y) < Epsilon ? 0.0f : physics.LinearVelocity.Y);
            if (physics.Anchored ||
                physics.LinearVelocity == Vector2.Zero && Math.Abs(physics.AngularVelocity) < Epsilon) return;

            if (ContainerHelpers.IsInContainer(entity) && physics.LinearVelocity != Vector2.Zero)
            {
                entity.Transform.Parent!.Owner.SendMessage(entity.Transform, new RelayMovementEntityMessage(entity));
                // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                physics.LinearVelocity = Vector2.Zero;
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
                var penetration = _physicsManager.CalculatePenetration(collision.A, collision.B);
                if (penetration > allowance)
                {
                    done = false;
                    var correction = collision.Normal * Math.Abs(penetration) * percent;
                    if (collision.APhysics != null && !((PhysicsComponent)collision.APhysics).Anchored && !collision.APhysics.Deleted)
                        collision.APhysics.Owner.Transform.WorldPosition -= correction;
                    if (collision.BPhysics != null && !((PhysicsComponent)collision.BPhysics).Anchored && !collision.BPhysics.Deleted)
                        collision.BPhysics.Owner.Transform.WorldPosition += correction;
                }
            }

            return done;
        }

        private float GetFriction(IEntity entity)
        {
            if (entity.HasComponent<CollidableComponent>() && entity.TryGetComponent(out PhysicsComponent physics) && physics.OnGround)
            {
                var location = entity.Transform;
                var grid = _mapManager.GetGrid(location.GridPosition.GridID);
                var tile = grid.GetTileRef(location.GridPosition);
                var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }
            return 0.0f;
        }
    }
}
