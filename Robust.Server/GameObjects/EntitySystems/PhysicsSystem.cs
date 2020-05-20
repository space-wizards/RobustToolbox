using System;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class PhysicsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPhysicsManager _physicsManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        private const float Epsilon = 1.0e-6f;

        private List<Manifold> _collisionCache = new List<Manifold>();

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, RelevantEntities.Where(e => !e.Deleted).ToList());
        }

        private void SimulateWorld(float frameTime, ICollection<IEntity> entities)
        {
            // simulation can introduce deleted entities into the query results
            foreach (var entity in entities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }

                ResolveImpulse(entity, frameTime);
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions();

            const float solveIterations = 4.0f;

            for (var i = 0; i < solveIterations; i++)
            {
                foreach (var entity in entities)
                {
                    UpdatePosition(entity, frameTime / solveIterations);
                }
                FixClipping(_collisionCache);
            }
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions()
        {
            _collisionCache.Clear();
            //var collisionsWith = new Dictionary<ICollideBehavior, int>();
            var physicsComponents = new Dictionary<ICollidableComponent, PhysicsComponent>();
            var combinations = new List<(EntityUid, EntityUid)>();
            var entities = RelevantEntities.ToList();
            foreach (var entity in entities)
            {
                if (entity.Deleted) continue;
                if (entity.TryGetComponent<CollidableComponent>(out var a))
                {
                    foreach (var b in a.GetCollidingEntities(Vector2.Zero).Select(e => e.GetComponent<CollidableComponent>()))
                    {
                        if (combinations.Contains((a.Owner.Uid, b.Owner.Uid)) ||
                            combinations.Contains((b.Owner.Uid, a.Owner.Uid))) continue;
                        combinations.Add((a.Owner.Uid, b.Owner.Uid));
                        if (a.Owner.TryGetComponent<PhysicsComponent>(out var aPhysics))
                        {
                            physicsComponents[a] = aPhysics;
                            if (b.Owner.TryGetComponent<PhysicsComponent>(out var bPhysics))
                            {
                                physicsComponents[b] = bPhysics;
                                _collisionCache.Add(new Manifold(a, b, aPhysics, bPhysics));
                            }
                            else
                            {
                                _collisionCache.Add(new Manifold(a, b, aPhysics, null));
                            }
                        }
                        else if (b.Owner.TryGetComponent<PhysicsComponent>(out var bPhysics))
                        {
                            _collisionCache.Add(new Manifold(a, b, null, bPhysics));
                        }
                    }
                }
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
        }

        private bool GetNextCollision(List<Manifold> collisions, int counter, out Manifold collision)
        {
            // The +1000 is completely arbitrary
            if (counter > collisions.Count + 1000)
            {
                collision = collisions[0];
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

            collision = collisions[0];
            return false;
        }

        private void ResolveImpulse(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();

            // Run the virtual controller
            physics.Controller?.UpdateBeforeProcessing();

            // Next, calculate frictional impulse
            var friction = GetFriction(entity);

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, physics.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant
            var frictionImpulse = physics.LinearVelocity == Vector2.Zero ? Vector2.Zero : physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionImpulse;
        }

        private void UpdatePosition(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();
            physics.LinearVelocity = new Vector2(Math.Abs(physics.LinearVelocity.X) < Epsilon ? 0.0f : physics.LinearVelocity.X, Math.Abs(physics.LinearVelocity.Y) < Epsilon ? 0.0f : physics.LinearVelocity.Y);
            if (physics.Anchored || (physics.LinearVelocity == Vector2.Zero && Math.Abs(physics.AngularVelocity) < Epsilon)) return;

            physics.Owner.Transform.WorldRotation += physics.AngularVelocity * frameTime;
            physics.Owner.Transform.WorldPosition += physics.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private void FixClipping(List<Manifold> collisions)
        {
            const float allowance = 0.05f;
            const float percent = 0.4f;
            foreach (var collision in collisions)
            {
                var penetration = _physicsManager.CalculatePenetration(collision.A, collision.B);
                if (penetration > allowance)
                {
                    var correction = collision.Normal * Math.Abs(penetration) * percent;
                    if (collision.APhysics != null)
                        collision.APhysics.Owner.Transform.WorldPosition -= correction;
                    if (collision.BPhysics != null)
                        collision.BPhysics.Owner.Transform.WorldPosition += correction;
                }
            }
        }

        private float GetFriction(IEntity entity)
        {
            if (entity.TryGetComponent(out CollidableComponent collider) && entity.TryGetComponent(out PhysicsComponent physics) && physics.OnGround)
            {
                var location = entity.Transform;
                var grid = _mapManager.GetGrid(location.GridPosition.GridID);
                var tile = grid.GetTileRef(location.GridPosition);
                var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }
            return 0;
        }

        private static readonly IComparer<Vector2> LengthComparer =
            Comparer<Vector2>.Create((a, b) => a.LengthSquared.CompareTo(b.LengthSquared));
    }
}
