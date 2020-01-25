using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;

namespace Robust.Shared.Interfaces.GameObjects
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
        void Update(float frameTime);

        /// <summary>
        ///     Client-specific per-render frame updating.
        /// </summary>
        void FrameUpdate(float frameTime);

        IComponentManager ComponentManager { get; }
        IEntityNetworkManager EntityNetManager { get; }
        IEventBus EventBus { get; }

        #region Entity Management

        IEntity CreateEntityUninitialized(string prototypeName);

        IEntity CreateEntityUninitialized(string prototypeName, GridCoordinates coordinates);

        IEntity CreateEntityUninitialized(string prototypeName, MapCoordinates coordinates);

        /// <summary>
        /// Spawns an initialized entity at the default location, using the given prototype.
        /// </summary>
        /// <param name="protoName">The prototype to clone. If this is null, the entity won't have a prototype.</param>
        /// <param name="coordinates"></param>
        /// <returns>Newly created entity.</returns>
        IEntity SpawnEntity(string protoName, GridCoordinates coordinates);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="protoName"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        IEntity SpawnEntity(string protoName, MapCoordinates coordinates);

        /// <summary>
        /// Spawns an initialized entity at the default location, using the given prototype.
        /// </summary>
        /// <remarks>
        ///     Does not run map init. This only matters on the server.
        /// </remarks>
        /// <param name="protoName">The prototype to clone. If this is null, the entity won't have a prototype.</param>
        /// <param name="coordinates"></param>
        /// <returns>Newly created entity.</returns>
        IEntity SpawnEntityNoMapInit(string protoName, GridCoordinates coordinates);

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        IEntity GetEntity(EntityUid uid);

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        bool TryGetEntity(EntityUid uid, out IEntity entity);

        /// <summary>
        /// Returns all entities that match with the provided query.
        /// </summary>
        /// <param name="query">The query to test.</param>
        /// <returns>An enumerable over all matching entities.</returns>
        IEnumerable<IEntity> GetEntities(IEntityQuery query);

        IEnumerable<IEntity> GetEntities();

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

        #region ComponentEvents

        /// <summary>
        /// Converts a raw NetIncomingMessage to an IncomingEntityMessage object
        /// </summary>
        /// <param name="message">raw network message</param>
        /// <returns>An IncomingEntityMessage object</returns>
        void HandleEntityNetworkMessage(MsgEntity message);
        #endregion ComponentEvents

        #region Spatial Queries

        IEnumerable<IEntity> GetEntitiesAt(Vector2 position);

        /// <summary>
        /// Checks if any entity is intersecting the box
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="box"></param>
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box);

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
        /// Gets entities with a bounding box that intersects this point
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point in coordinate form
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(GridCoordinates position);

        /// <summary>
        /// Gets entities that intersect with this entity
        /// </summary>
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
        /// <param name="arcWidth"></param>
        /// <returns></returns>
        IEnumerable<IEntity> GetEntitiesInArc(GridCoordinates coordinates, float range, Angle direction, float arcWidth);

        #endregion
    }
}
