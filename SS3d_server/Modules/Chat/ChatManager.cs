using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;

using SS3d_server.Modules.Mobs;

namespace SS3d_server.Modules.Chat
{
    public class ChatManager
    {
        private SS3DNetserver netServer;
        private MobManager mobManager;
        
        public ChatManager(SS3DNetserver _netServer, MobManager _mobManager)
        {
            netServer = _netServer;
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            ushort channel = message.ReadUInt16();
            string text = message.ReadString();
            string name = netServer.clientList[message.SenderConnection].playerName;

            SendChatMessage(channel, text, name);
        }

        public void SendChatMessage(ushort channel, string text, string name)
        {
            string fullmsg = name + ": " + text;


            NetOutgoingMessage message = netServer.netServer.CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write(channel);
            message.Write(fullmsg);

            netServer.SendMessageToAll(message);
        }
    }
}
