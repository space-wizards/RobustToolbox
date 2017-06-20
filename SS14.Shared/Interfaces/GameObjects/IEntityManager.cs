using Lidgren.Network;
using System;
using System.Collections.Generic;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IEntityManager
    {
        IList<ComponentFamily> SynchedComponentTypes { get; }

        void InitializeEntities();
        void LoadEntities();
        void Shutdown();
        void Update(float frameTime);
        void HandleEntityNetworkMessage(NetIncomingMessage msg);

        # region Entity Management

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        IEntity GetEntity(int eid);

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="eid">The entity ID to look up.</param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        bool TryGetEntity(int eid, out IEntity entity);

        /// <summary>
        /// Returns all entities that match with the provided query.
        /// </summary>
        /// <param name="query">The query to test.</param>
        /// <returns>An enumerable over all matching entities.</returns>
        IEnumerable<IEntity> GetEntities(IEntityQuery query);

        /// <summary>
        /// Shuts-down and removes given <see cref="IEntity"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(IEntity e);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists(int eid);

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

        void SubscribeEvent<T>(Delegate eventHandler, IEntityEventSubscriber s) where T : EntityEventArgs;

        void UnsubscribeEvent<T>(IEntityEventSubscriber s) where T : EntityEventArgs;

        void UnsubscribeEvent(Type eventType, Delegate evh, IEntityEventSubscriber s);

        void RaiseEvent(object sender, EntityEventArgs toRaise);

        void RemoveSubscribedEvents(IEntityEventSubscriber subscriber);

        #endregion ComponentEvents

        #region State stuff

        void ApplyEntityStates(List<EntityState> entityStates, float serverTime);

        #endregion State stuff
    }
}
