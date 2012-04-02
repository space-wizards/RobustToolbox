using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using Lidgren.Network;

namespace ServerInterfaces.Chat
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int entityID);
        void Initialize(ISS13Server server);
        void HandleNetMessage(NetIncomingMessage message);
    }
}
