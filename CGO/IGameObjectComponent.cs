using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace CGO
{
    public interface IGameObjectComponent
    {
        Entity Owner { get; set; }

        ComponentReplyMessage RecieveMessage(object sender, MessageType type, params object[] list);
        void OnRemove();
        void OnAdd(Entity owner);
        void Update(float frameTime);
        void Shutdown();
        ComponentFamily Family {get;set;}
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message);
    }
}
