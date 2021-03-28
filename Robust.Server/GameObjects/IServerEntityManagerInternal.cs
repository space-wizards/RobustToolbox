using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    internal interface IServerEntityManagerInternal : IServerEntityManager
    {
        // These methods are used by the map loader to do multi-stage entity construction during map load.
        // I would recommend you refer to the MapLoader for usage.

        IEntity AllocEntity(string? prototypeName, EntityUid? uid = null);

        void FinishEntityLoad(IEntity entity, IEntityLoadContext? context = null);

        void FinishEntityInitialization(IEntity entity);

        void FinishEntityStartup(IEntity entity);
    }
}
