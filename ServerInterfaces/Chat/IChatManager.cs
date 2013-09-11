using Lidgren.Network;
using SS13_Shared;

namespace ServerInterfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int? entityID);
        void Initialize(ISS13Server server);
        void HandleNetMessage(NetIncomingMessage message);
    }
}