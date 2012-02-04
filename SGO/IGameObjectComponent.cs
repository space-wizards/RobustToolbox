using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using Lidgren.Network;

namespace SGO
{
    public interface IGameObjectComponent
    {
        Entity Owner { get; set; }

        void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list);
        void OnRemove();
        void OnAdd(Entity owner);
        void Update(float frameTime);
        void Shutdown();
        ComponentFamily Family {get;}
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client);
        void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection);
    }
}
