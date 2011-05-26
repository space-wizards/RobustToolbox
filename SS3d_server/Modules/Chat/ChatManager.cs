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
        
        public ChatManager(SS3DNetserver _netServer)
        {
            netServer = _netServer;
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            ushort channel = message.ReadUInt16();
            string text = message.ReadString();
            string name = netServer.clientList[message.SenderConnection].playerName;
            //ushort mobID = netServer.clientList[message.SenderConnection].mobID;
            ushort atomID = netServer.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom.uid;
            if (atomID == null)
                atomID = 0;

            SendChatMessage(channel, text, name, atomID);
        }

        public void SendChatMessage(ushort channel, string text, string name, ushort atomID)
        {
            string fullmsg = name + ": " + text;


            NetOutgoingMessage message = netServer.netServer.CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write(channel);
            message.Write(fullmsg);
            message.Write(atomID);

            netServer.SendMessageToAll(message);
        }
    }
}
