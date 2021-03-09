using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Prometheus;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public interface IEntityManager
    {
        /// <summary>
        ///     The current simulation tick being processed.
        /// </summary>
        GameTick CurrentTick { get; }

        void Initialize();
        void Startup();
        void Shutdown();
        void Update(float frameTime, Histogram? histogram=null);

        /// <summary>
        ///     Client-specific per-render frame updating.
        /// </summary>
        void FrameUpdate(float frameTime);

        IComponentManager ComponentManager { get; }
        IEntityNetworkManager EntityNetManager { get; }
        IEntitySystemManager EntitySysManager { get; }
        IEventBus EventBus { get; }

        #region Entity Management

        event EventHandler<EntityUid>? EntityAdded;
        event EventHandler<EntityUid>? EntityInitialized;
        event EventHandler<EntityUid>? EntityDeleted;

        IEntity CreateEntityUninitialized(string? prototypeName);

        IEntity CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates);

        IEntity CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates);

        /// <summary>
        /// Spawns an initialized entity at the default location, using the given prototype.
        /// </summary>
        /// <param name="protoName">The prototype to clone. If this is null, the entity won't have a prototype.</param>
        /// <param name="coordinates"></param>
        /// <returns>Newly created entity.</returns>
        IEntity SpawnEntity(string? protoName, EntityCoordinates coordinates);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="protoName"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        IEntity SpawnEntity(string? protoName, MapCoordinates coordinates);

        /// <summary>
        /// Spawns an initialized entity at the default location, using the given prototype.
        /// </summary>
        /// <remarks>
        ///     Does not run map init. This only matters on the server.
        /// </remarks>
        /// <param name="protoName">The prototype to clone. If this is null, the entity won't have a prototype.</param>
        /// <param name="coordinates"></param>
        /// <returns>Newly created entity.</returns>
        IEntity SpawnEntityNoMapInit(string? protoName, EntityCoordinates coordinates);

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>Entity or throws if entity id doesn't exist</returns>
        IEntity GetEntity(EntityUid uid);

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        bool TryGetEntity(EntityUid uid, [NotNullWhen(true)] out IEntity? entity);

        /// <summary>
        /// Returns all entities that match with the provided query.
        /// </summary>
        /// <param name="query">The query to test.</param>
        /// <returns>An enumerable over all matching entities.</returns>
        IEnumerable<IEntity> GetEntities(IEntityQuery query);

        IEnumerable<IEntity> GetEntities();

        /// <summary>
        ///     Yields all of the entities on a particular map. faster than GetEntities()
        /// </summary>
        /// <param name="mapId"></param>
        /// <returns></returns>
        IEnumerable<IEntity> GetEntitiesInMap(MapId mapId);

        /// <summary>
        /// Shuts-down and removes given <see cref="IEntity"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(IEntity e);

        /// <summary>
        /// Shuts-down and removes the entity with the given <see cref="EntityUid"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="uid">Uid of entity to remove.</param>
        void DeleteEntity(EntityUid uid);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists(EntityUid uid);

        #endregion Entity Management

        #region Spatial Queries

        /// <summary>
        /// Gets entities with a origin at the position.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        /// <returns></returns>
        IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false);

        /// <summary>
        /// Checks if any entity is intersecting the box
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="box"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false);

        /// <summary>
        /// Gets entities with a bounding box that intersects this box
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point
        /// </summary>
        /// <param name="position"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, bool approximate = false);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point in coordinate form
        /// </summary>
        /// <param name="position"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, bool approximate = false);

        /// <summary>
        /// Gets entities that intersect with this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, bool approximate = false);

        /// <summary>
        /// Gets entities within a certain *square* range of this local coordinate
        /// </summary>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, bool approximate = false);

        /// <summary>
        /// Gets entities within a certain *square* range of this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="range"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, bool approximate = false);

        /// <summary>
        /// Gets whether two entities are intersecting each other
        /// </summary>
        /// <param name="entityOne"></param>
        /// <param name="entityTwo"></param>
        /// <returns></returns>
        public bool IsIntersecting(IEntity entityOne, IEntity entityTwo);

        /// <summary>
        /// Gets entities within a certain *square* range of this bounding box
        /// </summary>
        /// <param name="mapID"></param>
        /// <param name="box"></param>
        /// <param name="range"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float range, bool approximate = false);

        /// <summary>
        /// Get entities with bounding box in range of this whose center is within a certain directional arc, angle specifies center bisector of arc
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="range"></param>
        /// <param name="direction"></param>
        /// <param name="arcWidth"></param>
        /// <param name="approximate">If true, will not recalculate precise entity AABBs, resulting in a perf increase. </param>
        /// <returns></returns>
        IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction, float arcWidth, bool approximate = false);

        #endregion

        #region Spatial Updates

        bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null);
        bool RemoveFromEntityTree(IEntity entity, MapId mapId);

        #endregion

        void Update();

    }
}
