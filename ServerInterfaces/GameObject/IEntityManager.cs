using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ServerInterfaces.GOC
{
    public interface IEntityManager : GameObject.IEntityManager
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void SaveEntities();
        Entity SpawnEntity(string template, int uid = -1);
        Entity SpawnEntityAt(string entityTemplateName, Vector2 vector2);
        List<EntityState> GetEntityStates();
        void Update(float frameTime);
    }
}