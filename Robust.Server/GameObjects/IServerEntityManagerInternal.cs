using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    internal interface IServerEntityManagerInternal : IServerEntityManager
    {
        // These methods are used by the map loader to do multi-stage entity construction during map load.
        // I would recommend you refer to the MapLoader for usage.

        EntityUid AllocEntity(string? prototypeName, EntityUid uid = default);

        void FinishEntityLoad(EntityUid entity, IEntityLoadContext? context = null);

        void FinishEntityInitialization(EntityUid entity);

        void FinishEntityStartup(EntityUid entity);
    }
}
