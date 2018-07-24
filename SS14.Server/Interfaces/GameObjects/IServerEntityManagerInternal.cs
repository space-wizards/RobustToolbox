using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Serialization;

namespace SS14.Server.Interfaces.GameObjects
{
    interface IServerEntityManagerInternal : IServerEntityManager
    {
        /// <summary>
        ///     Creates a new entity from a prototype and allocates an UID,
        ///     but does not load data yet.
        /// </summary>
        IEntity AllocEntity(string prototypeName, EntityUid? uid = null);

        void FinishEntity(IEntity entity, IEntityFinishContext context = null);
    }
}
