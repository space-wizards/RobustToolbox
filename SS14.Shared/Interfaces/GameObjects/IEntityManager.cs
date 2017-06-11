using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IEntityManager : IIoCInterface
    {
        IList<ComponentFamily> SynchedComponentTypes { get; }

        # region Entity Management

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        IEntity GetEntity(int eid);

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
    }
}
