using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13_Shared.GO;

namespace ServerInterfaces.GameObject
{
    public interface IEntityManager
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void HandleNetworkMessage(NetIncomingMessage message);
        IEntity GetEntity(int id);
        void DeleteEntity(IEntity entity);
        void SendEntities(NetConnection connection);
        void SaveEntities();
        IEntity SpawnEntity(string template, bool send = true);
        IEntity SpawnEntityAt(string entityTemplateName, SS13_Shared.Vector2 vector2, bool send = true);
        ComponentFactory ComponentFactory { get; }
        List<EntityState> GetEntityStates();
        void Update(float frameTime);
    }
}
