using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerInterfaces
{
    public interface IChatManager
    {
        void SendChatMessage(ChatChannel channel, string text, string name, int entityID);
    }
}
