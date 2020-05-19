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
using Robust.Shared.Log;
using Robust.Shared.Physics;

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
#pragma warning restore 649

        private const float Epsilon = 1.0e-6f;

        private Dictionary<EntityUid, Vector2> _impulseCache = new Dictionary<EntityUid, Vector2>();

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, RelevantEntities.ToList());
        }

        private void SimulateWorld(float frameTime, ICollection<IEntity> entities)
        {

            // simulation can introduce deleted entities into the query results
            foreach (var entity in entities)
            {
                if (entity.Deleted)
                {
                    continue;
                }

                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }

                ResolveImpulse(entity, frameTime);
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions();

            foreach (var entity in entities)
            {
                if (entity.Deleted)
                {
                    continue;
                }

                UpdatePosition(entity, frameTime);
            }
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions()
        {
            _impulseCache.Clear();
            //var collisionsWith = new Dictionary<ICollideBehavior, int>();
            var collisionGroups = new List<AxisCollisionGroup>();
            var physicsComponents = new Dictionary<ICollidableComponent, PhysicsComponent>();
            var combinations = new List<(EntityUid, EntityUid)>();
            foreach (var entity in RelevantEntities)
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
                        }
                        if (b.Owner.TryGetComponent<PhysicsComponent>(out var bPhysics))
                        {
                            physicsComponents[b] = bPhysics;
                        }

                        var manifold = new Manifold(a, b, physicsComponents.ContainsKey(a) ? physicsComponents[a] : null, physicsComponents.ContainsKey(b) ? physicsComponents[b] : null);
                        if (manifold.Normal == Vector2.Zero) continue;
                        var inCollisionGroup = false;

                        for (var i = 0; i < collisionGroups.Count; i++)
                        {
                            if (collisionGroups[i].TryAddCollision(manifold))
                            {
                                inCollisionGroup = true;
                                break;
                            }
                        }

                        if (!inCollisionGroup)
                        {
                            collisionGroups.Add(new AxisCollisionGroup(manifold));
                        }
                    }
                }
            }

            foreach (var group in collisionGroups)
            {
                _physicsManager.SolveGroup(group);
            }

            /*
            foreach (var collisionCountPair in collisionsWith)
            {
                collisionCountPair.Key.PostCollide(collisionCountPair.Value);
            }
            */
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

            const float solveIterations = 4.0f;

            for (var _ = 0.0f; _ < solveIterations; _++)
            {
                physics.Owner.Transform.WorldRotation += physics.AngularVelocity * frameTime / solveIterations;
                physics.Owner.Transform.WorldPosition += physics.LinearVelocity * frameTime / solveIterations;
            }
        }

        private void FixClipping(PhysicsComponent physics)
        {
            if (physics.Owner.TryGetComponent<CollidableComponent>(out var collider))
            {
                var entities = collider.GetCollidingEntities(Vector2.Zero).ToList();
                if (!entities.Any()) return;
                foreach (var clippingCollider in entities.Select(e => e.GetComponent<CollidableComponent>()))
                {
                    var normal = -_physicsManager.CalculateNormal(collider, clippingCollider);
                    var iterations = 2;
                    while (PhysicsManager.CollidesOnMask(collider, clippingCollider) && iterations > 0)
                    {
                        iterations--;
                        collider.Owner.Transform.WorldPosition += normal * 0.005f;
                    }
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
