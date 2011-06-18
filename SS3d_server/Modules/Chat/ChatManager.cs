using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;

//using SS3d_server.Modules.Mobs;

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

            text = text.Trim(); // Remove whitespace
            if (text[0] == '/')
                ProcessCommand(text, name, channel, atomID);
            else
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

        /// <summary>
        /// Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">chat text</param>
        /// <param name="name">player name that sent the chat text</param>
        /// <param name="channel">channel message was recieved on</param>
        /// <param name="atomID">uid of the atom that sent the message. This will always be a player's attached atom</param>
        private void ProcessCommand(string text, string name, ushort channel, ushort atomID)
        {
            List<string> args = new List<string>();

            ParseArguments(text, args);

            string command = args[0];

            Vector3 position;
            Atom.Atom player;

            switch (command)
            {
                case "crowbar":
                    player = netServer.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector3(0, 0, 0);
                    else
                        position = player.position;
                    netServer.atomManager.SpawnAtom("Atom.Item.Tool.Crowbar", position);                   
                    break;
                case "toolbox":
                    player = netServer.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector3(0, 0, 0);
                    else
                        position = player.position;
                    netServer.atomManager.SpawnAtom("Atom.Item.Container.Toolbox", position);  
                    break;
                default:
                    string message = "Command '" + command + "' not recognized.";
                    SendChatMessage(channel, message, name, atomID);
                    break;
            }
        }

        /// <summary>
        /// Command parsing func
        /// </summary>
        /// <param name="text">full input string</param>
        /// <param name="args">List of arguments, including the command as #0</param>
        private void ParseArguments(string text, List<string> args)
        {
            string buf = "";
            bool inquotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '/')
                    continue;
                else if (inquotes && text[i] == '"')
                {
                    inquotes = false;
                    args.Add(buf);
                    buf = "";
                    i++;//skip the following space.
                    continue;
                }
                else if (!inquotes && text[i] == '"')
                {
                    inquotes = true;
                    continue;
                }
                else if (text[i] == ' ' && !inquotes)
                {
                    args.Add(buf);
                    buf = "";
                    continue;
                }
                else
                {
                    buf += text[i];
                    continue;
                }
            }

            if (buf != "")
                args.Add(buf);

        }
    }
}
