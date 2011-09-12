using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;

//using SS3d_server.Modules.Mobs;

namespace SS3D_Server.Modules.Chat
{
    public class ChatManager
    {
        public ChatManager()
        {
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            ChatChannel channel = (ChatChannel)message.ReadByte();
            string text = message.ReadString();
            string name = SS3DServer.Singleton.clientList[message.SenderConnection].playerName;
            LogManager.Log("CHAT- Channel " + channel.ToString() +  " - Player " + name + "Message: " + text + "\n");

            ushort atomID = 0;
            if (SS3DServer.Singleton.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom != null)
                atomID = SS3DServer.Singleton.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom.uid;

            text = text.Trim(); // Remove whitespace
            if (text[0] == '/')
                ProcessCommand(text, name, channel, atomID, message.SenderConnection);
            else
                SendChatMessage(channel, text, name, atomID);
        }

        public void SendChatMessage(ChatChannel channel, string text, string name, ushort atomID)
        {
            string fullmsg = name + ": " + text;

            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)channel);
            message.Write(fullmsg);
            message.Write(atomID);

            SS3DServer.Singleton.SendMessageToAll(message);
        }

        /// <summary>
        /// Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">chat text</param>
        /// <param name="name">player name that sent the chat text</param>
        /// <param name="channel">channel message was recieved on</param>
        /// <param name="atomID">uid of the atom that sent the message. This will always be a player's attached atom</param>
        private void ProcessCommand(string text, string name, ChatChannel channel, ushort atomID, NetConnection client)
        {
            List<string> args = new List<string>();

            ParseArguments(text, args);

            string command = args[0];

            Vector2 position;
            Atom.Atom player;

            switch (command)
            {
                case "crowbar":
                    player = SS3DServer.Singleton.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector2(0, 0);
                    else
                        position = player.position;
                    SS3DServer.Singleton.atomManager.SpawnAtom("Atom.Item.Tool.Crowbar", position);                   
                    break;
                case "toolbox":
                    player = SS3DServer.Singleton.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector2(0, 0);
                    else
                        position = player.position;
                    SS3DServer.Singleton.atomManager.SpawnAtom("Atom.Item.Container.Toolbox", position);  
                    break;
                case "addgas":
                    player = SS3DServer.Singleton.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector2(0,0);
                    else
                        position = player.position;

                    if (args.Count > 1 && Convert.ToInt32(args[1]) > 0)
                    {
                        int amount = Convert.ToInt32(args[1]);
                        SS3DServer.Singleton.map.AddGasAt(SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(position), GasType.Toxin, amount);
                    }
                    break;
                case "gasreport":
                    player = SS3DServer.Singleton.atomManager.GetAtom(atomID);
                    if (player == null)
                        position = new Vector2(0,0);
                    else
                        position = player.position;

                    var p = SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(position);
                    var c = SS3DServer.Singleton.map.GetTileAt(p.x, p.y).gasCell;
                    foreach(var g in c.gasses)
                    {
                        SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Default, g.Key.ToString() + ": " + g.Value.ToString(), "GasReport", 0);
                    }
                    
                    break;
                case "sprayblood":
                    player = SS3DServer.Singleton.atomManager.GetAtom(atomID);
                    if (player == null)
                        return;
                    else
                        position = player.position;
                    p = SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(position);
                    var t = SS3DServer.Singleton.map.GetTileAt(p.x, p.y);
                    if (args.Count > 1 && Convert.ToInt32(args[1]) > 0)
                    {
                        for (int i = 0; i <= Convert.ToInt32(args[1]); i++)
                        {
                            t.AddDecal(DecalType.Blood);
                        }
                    }
                    else
                        t.AddDecal(DecalType.Blood);
                        
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
