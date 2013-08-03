using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13_Shared.GO;

namespace ServerInterfaces.GOC
{
    public interface IEntityManager: GameObject.IEntityManager
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void SendEntities(NetConnection connection);
        void SaveEntities();
        Entity SpawnEntity(string template, bool send = true);
        Entity SpawnEntityAt(string entityTemplateName, SS13_Shared.Vector2 vector2, bool send = true);
        List<EntityState> GetEntityStates();
        void Update(float frameTime);
    }
}
