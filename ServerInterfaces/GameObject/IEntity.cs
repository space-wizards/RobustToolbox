using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;
using SS13_Shared.GO.Server;
using System.Collections.Generic;

namespace ServerInterfaces.GameObject
{
    public delegate void ShutdownEvent(IEntity e);
    
    public interface IEntity
    {
        void Translate(Vector2 toPosition);
        Vector2 Position { get; set; }
        int Rotation { get; set; }
        int Uid { get; set; }
        string Name { get; set; }

        void SendMessage(object sender, ComponentMessageType type, params object[] parameters);
        void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args);
        ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type, params object[] args);

        void Shutdown();
        void FireNetworkedSpawn();
        void FireNetworkedJoinSpawn(NetConnection client);
        void AddComponent(ComponentFamily family, IGameObjectComponent component);
        void RemoveComponent(ComponentFamily family);
        IEntityTemplate Template { get; set; }
        event EntityMoveEvent OnMove; 
        event ShutdownEvent OnShutdown;
        bool HasComponent(ComponentFamily family);
        IGameObjectComponent GetComponent(ComponentFamily componentFamily);
        void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method, NetConnection recipient, params object[] messageParams);

        void Initialize(bool loaded = false);
        void Moved(Vector2 oldPosition);
        void HandleNetworkMessage(ServerIncomingEntityMessage message);
    }
}
