using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO
{
    public interface IGameObjectComponent
    {
        Entity Owner { get; set; }
        ComponentFamily Family { get; }

        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list);
        void OnRemove();
        void OnAdd(Entity owner);
        void Update(float frameTime);
        void Shutdown();
        void SetParameter(ComponentParameter parameter);
        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client);
        void HandleInstantiationMessage(NetConnection netConnection);
    }
}