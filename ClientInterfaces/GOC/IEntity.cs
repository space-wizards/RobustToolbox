using System;
using System.Collections.Generic;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IEntity
    {
        IEntityTemplate Template { get; set; }
        string Name { get; set; }
        int Uid { get; set; }
        Vector2D Position { get; set; }
        bool Initialized { get; set; }
        event EventHandler<VectorEventArgs> OnMove;
        void AddComponent(ComponentFamily family, IGameObjectComponent component);
        void RemoveComponent(ComponentFamily family);
        IGameObjectComponent GetComponent(ComponentFamily family);
        bool HasComponent(ComponentFamily family);
        string GetDescriptionString();
        void Initialize();
        void Moved();
        void Shutdown();
        void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args);
        void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method, params object[] messageParams);
        void SendComponentInstantiationMessage(IGameObjectComponent component);
        void HandleNetworkMessage(IncomingEntityMessage message);
    }
}
