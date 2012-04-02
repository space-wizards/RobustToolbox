using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace ServerInterfaces.GameObject
{
    public interface IGameObjectComponent
    {
        IEntity Owner { get; set; }

        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list);
        void OnRemove();
        void OnAdd(IEntity owner);
        void Update(float frameTime);
        void Shutdown();
        ComponentFamily Family {get;}
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client);
        void HandleInstantiationMessage(Lidgren.Network.NetConnection netConnection);
    }
}
