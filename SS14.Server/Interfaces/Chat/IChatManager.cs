using Lidgren.Network;
using SS14.Shared;
using System.Collections.Generic;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int? entityID);
        void SendPrivateMessage(IClient client, ChatChannel channel, string text, string name, int? entityId);
        void Initialize(ISS14Server server);
        void HandleNetMessage(NetIncomingMessage message);

        IDictionary<string, IChatCommand> Commands { get; }
    }
}
