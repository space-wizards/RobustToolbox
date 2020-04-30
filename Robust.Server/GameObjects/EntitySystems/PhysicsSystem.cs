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

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, RelevantEntities);
        }

        private void SimulateWorld(float frameTime, IEnumerable<IEntity> entities)
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

            foreach (var entity in entities)
            {
                if (entity.Deleted)
                {
                    continue;
                }

                UpdatePosition(entity, frameTime);
            }
        }

        private void ResolveImpulse(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();

            // Run the virtual controller
            physics.Controller?.UpdateBeforeProcessing();

            // Next, calculate frictional impulse
            var friction = GetFriction(entity);

            // No multiplication/division by mass here since that would be redundant
            var frictionImpulse = physics.LinearVelocity == Vector2.Zero ? Vector2.Zero : physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionImpulse;

            var collisionImpulse = Vector2.Zero;
            // Calculate collision forces
            if (entity.TryGetComponent<CollidableComponent>(out var collider))
            {
                var collidables = collider.GetCollidingEntities(Vector2.Zero);
                // Run collision behavior
                var collidecomponents = entity.GetAllComponents<ICollideBehavior>().ToList();

                for (var i = 0; i < collidecomponents.Count; i++)
                {
                    collidecomponents[i].CollideWith(collidables.Select(c => (c as IPhysBody).Owner).ToList());
                }
                foreach (var otherCollider in collidables)
                {
                    if (((IComponent) otherCollider).Owner.TryGetComponent<PhysicsComponent>(out var sourcePhysics))
                    {
                        collisionImpulse += _physicsManager.CalculateCollisionImpulse(collider, otherCollider, physics.LinearVelocity, sourcePhysics.LinearVelocity, physics.Mass, sourcePhysics.Mass);
                    }
                    else
                    {
                        collisionImpulse += _physicsManager.CalculateCollisionImpulse(collider, otherCollider,
                            physics.LinearVelocity, Vector2.Zero, physics.Mass, 0.0f);
                    }
                }
            }
            physics.LinearVelocity += collisionImpulse / physics.Mass;
            // Won't be affected by impulses
            if (physics.Anchored) physics.LinearVelocity = Vector2.Zero;
            physics.Controller?.UpdateAfterProcessing();
        }

        private void UpdatePosition(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();

            physics.Owner.Transform.WorldPosition += physics.LinearVelocity * frameTime;
            physics.Owner.Transform.WorldRotation += physics.AngularVelocity * frameTime;
        }

        private float GetFriction(IEntity entity)
        {
            if (entity.TryGetComponent(out CollidableComponent collider) && entity.TryGetComponent(out PhysicsComponent physics) && physics.IsOnGround())
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
