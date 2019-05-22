using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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

        #region Entity Management

        /// <summary>
        /// Spawns an initialized entity at the default location, using the given prototype.
        /// </summary>
        /// <param name="protoName">The prototype to clone. If this is null, the entity won't have a prototype.</param>
        /// <returns>Newly created entity.</returns>
        IEntity SpawnEntity(string protoName);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string entityType, GridCoordinates coordinates);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="position"></param>
        /// <param name="argMap"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="position"></param>
        /// <param name="argMap"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string entityType, GridCoordinates coordinates, out IEntity entity);

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

        IEnumerable<IEntity> GetEntitiesAt(Vector2 position);

        /// <summary>
        /// Shuts-down and removes given <see cref="IEntity"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(IEntity e);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists(EntityUid uid);

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        void FlushEntities();

        /// <summary>
        /// Retrieves template with given name from db
        /// </summary>
        /// <param name="prototypeName">name of the template</param>
        /// <returns>Template</returns>
        EntityPrototype GetTemplate(string prototypeName);

        #endregion Entity Management

        #region ComponentEvents

        void SubscribeEvent<T>(EntityEventHandler<T> eventHandler, IEntityEventSubscriber s)
            where T : EntityEventArgs;

        void UnsubscribeEvent<T>(IEntityEventSubscriber s)
            where T : EntityEventArgs;

        void RaiseEvent(object sender, EntityEventArgs toRaise);
        void QueueEvent(object sender, EntityEventArgs toRaise);

        void RemoveSubscribedEvents(IEntityEventSubscriber subscriber);

        #endregion ComponentEvents
    }
}
