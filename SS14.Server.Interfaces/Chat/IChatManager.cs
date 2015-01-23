using Lidgren.Network;
using SS14.Shared;

namespace SS14.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int? entityID);
        void Initialize(ISS14Server server);
        void HandleNetMessage(NetIncomingMessage message);
    }
}