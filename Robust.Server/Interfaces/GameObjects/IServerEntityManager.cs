using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager
    {
        /// <summary>
        /// Gets entities with a bounding box that intersects this box
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point in coordinate form
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(GridCoordinates position);

        /// <summary>
        /// Gets entities that intersect with this entity
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity);

        /// <summary>
        /// Gets entities within a certain *square* range of this local coordinate
        /// </summary>
        /// <param name="position"></param>
        /// <param name="range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float range);

        /// <summary>
        /// Gets entities within a certain *square* range of this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range);

        /// <summary>
        /// Gets entities within a certain *square* range of this bounding box
        /// </summary>
        /// <param name="mapID"></param>
        /// <param name="box"></param>
        /// <param name="range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float range);

        /// <summary>
        /// Get entities with bounding box in range of this whose center is within a certain directional arc, angle specifies center bisector of arc
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="range"></param>
        /// <param name="direction"></param>
        /// <param name="arcwidth"></param>
        /// <returns></returns>
        IEnumerable<IEntity> GetEntitiesInArc(GridCoordinates coordinates, float range, Angle direction, float arcwidth);

        /// <summary>
        ///     Gets all entity states that have been modified after and including the provided tick.
        /// </summary>
        List<EntityState> GetEntityStates(uint fromTick);

        // Keep track of deleted entities so we can sync deletions with the client.
        /// <summary>
        ///     Gets a list of all entity UIDs that were deleted between <paramref name="fromTick" /> and now.
        /// </summary>
        List<EntityUid> GetDeletedEntities(uint fromTick);

        /// <summary>
        ///     Remove deletion history.
        /// </summary>
        /// <param name="toTick">The last tick to delete the history for. Inclusive.</param>
        void CullDeletionHistory(uint toTick);
    }
}
