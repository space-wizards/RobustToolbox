using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Serialization;

namespace SS14.Server.Interfaces.GameObjects
{
    interface IServerEntityManagerInternal : IServerEntityManager
    {
        IEntity AllocEntity(string prototypeName, EntityUid? uid = null);

        void FinishEntityLoad(IEntity entity, IEntityLoadContext context = null);

        void FinishEntityInitialization(IEntity entity);

        void FinishEntityStartup(IEntity entity);
    }
}
