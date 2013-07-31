using System;
using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IGameObjectComponent: GameObject.IComponent
    {
        //void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list);
        Type StateType { get; }

        void HandleComponentState(dynamic compState);
    }
}
