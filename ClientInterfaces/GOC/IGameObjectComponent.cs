using System;
using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IGameObjectComponent: GameObject.IComponent
    {
        //void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list);
        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list);
        void Update(float frameTime);
        Type StateType { get; }
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message);

        void HandleComponentState(dynamic compState);
    }
}
