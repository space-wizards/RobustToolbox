using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IGameObjectComponent
    {
        IEntity Owner { get; set; }

        //void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list);
        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list);
        void OnRemove();
        void OnAdd(IEntity owner);
        void Update(float frameTime);
        void Shutdown();
        ComponentFamily Family {get;}
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message);
    }
}
