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

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class PhysicsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
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

                ResolveForces(_mapManager, _tileDefinitionManager, entity, frameTime);
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

        private void ResolveForces(IMapManager mapManager, ITileDefinitionManager tileDefinitionManager, IEntity entity,
            float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();

            // TODO: Everything
        }

        private void UpdatePosition(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<PhysicsComponent>();

            // TODO: Everything
        }

        private static float GetFriction(ITileDefinitionManager tileDefinitionManager, IMapManager mapManager, IEntity entity)
        {
            if (entity.TryGetComponent(out CollidableComponent collider) && collider.IsScrapingFloor)
            {
                var location = entity.Transform;
                var grid = mapManager.GetGrid(location.GridPosition.GridID);
                var tile = grid.GetTileRef(location.GridPosition);
                var tileDef = tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }
            return 0;
        }

        private static readonly IComparer<Vector2> LengthComparer =
            Comparer<Vector2>.Create((a, b) => a.LengthSquared.CompareTo(b.LengthSquared));
    }
}
