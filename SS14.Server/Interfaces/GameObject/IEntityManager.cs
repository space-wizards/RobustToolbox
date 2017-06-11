using Lidgren.Network;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GOC
{
    public interface IEntityManager : Shared.Interfaces.GameObjects.IEntityManager, IIoCInterface
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void SaveEntities();
        IEntity SpawnEntity(string template, int uid = -1);
        IEntity SpawnEntityAt(string entityTemplateName, Vector2f vector2);
        IList<EntityState> GetEntityStates();
        void Update(float frameTime);
    }
}
