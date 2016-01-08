using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Collections.Generic;
using SS14.Shared.Maths;
using SFML.System;

namespace SS14.Server.Interfaces.GOC
{
    public interface IEntityManager : SS14.Shared.GameObjects.IEntityManager
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void SaveEntities();
        Entity SpawnEntity(string template, int uid = -1);
        Entity SpawnEntityAt(string entityTemplateName, Vector2f vector2);
        List<EntityState> GetEntityStates();
        void Update(float frameTime);
    }
}