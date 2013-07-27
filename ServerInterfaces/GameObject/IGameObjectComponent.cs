using System.Collections.Generic;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace ServerInterfaces.GameObject
{
    public interface IGameObjectComponent: IComponent
    {

        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list);
        void Update(float frameTime);
        ComponentFamily Family {get;}
        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client);
        void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection);
        void SetSVar(MarshalComponentParameter sVar);
        List<MarshalComponentParameter> GetSVars();
        ComponentState GetComponentState();

    }
}
