using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Interfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int? entityID);
        void SendPrivateMessage(INetChannel client, ChatChannel channel, string text, string name, int? entityId);
        void Initialize();
        void HandleNetMessage(MsgChat message);
    }
}
