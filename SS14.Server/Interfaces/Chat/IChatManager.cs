using Lidgren.Network;
using SS14.Shared;
using System.Collections.Generic;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int? entityID);
        void SendPrivateMessage(NetChannel client, ChatChannel channel, string text, string name, int? entityId);
        void Initialize();
        void HandleNetMessage(MsgChat message);

        IDictionary<string, IChatCommand> Commands { get; }
    }
}
