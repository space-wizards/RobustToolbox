using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    internal interface IClientEntityManagerInternal : IClientEntityManager
    {
        // These methods are used by the Game State Manager.

        IEntity CreateEntity(string? prototypeName, EntityUid? uid = null);

        void InitializeEntity(IEntity entity);

        void StartEntity(IEntity entity);
    }
}
