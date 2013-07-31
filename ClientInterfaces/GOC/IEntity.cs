using System;
using System.Collections.Generic;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IEntity : GameObject.IEntity
    {
        IEntityTemplate Template { get; set; }
        int Uid { get; set; }
        bool Initialized { get; set; }
        event EventHandler<VectorEventArgs> OnMove;
        string GetDescriptionString();
        void Initialize();
        void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args);
        void SendMessage(object sender, ComponentMessageType type, params object[] args);
        ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type, params object[] args);
        void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method, params object[] messageParams);
        void SendComponentInstantiationMessage(IGameObjectComponent component);
        void HandleNetworkMessage(IncomingEntityMessage message);
        void GetSVars();
        void SetSVar(MarshalComponentParameter svar);
        event EventHandler<GetSVarsEventArgs> GetSVarsCallback;
        Vector2D Velocity { get; set; }
        void HandleEntityState(EntityState state);
    }
}
