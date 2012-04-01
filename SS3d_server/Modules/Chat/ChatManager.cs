using System;
using System.Collections.Generic;

using Lidgren.Network;
using SS13_Shared;
using ServerServices;
using ServerInterfaces;
using SGO;
using ServerServices.Log;

namespace SS13_Server.Modules.Chat
{
    public class ChatManager : IService, IChatManager
    {
        public ChatManager()
        {
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            ChatChannel channel = (ChatChannel)message.ReadByte();
            string text = message.ReadString();
            string name = SS13Server.Singleton.ClientList[message.SenderConnection].playerName;
            LogManager.Log("CHAT- Channel " + channel.ToString() +  " - Player " + name + "Message: " + text + "\n");

            int entityId = 0;
            if (SS13Server.Singleton.PlayerManager.GetSessionByConnection(message.SenderConnection).attachedEntity != null)
                entityId = SS13Server.Singleton.PlayerManager.GetSessionByConnection(message.SenderConnection).attachedEntity.Uid;

            text = text.Trim(); // Remove whitespace
            if (text[0] == '/')
                ProcessCommand(text, name, channel, entityId, message.SenderConnection);
            else
                SendChatMessage(channel, text, name, entityId);
        }

        public void SendChatMessage(ChatChannel channel, string text, string name, int entityId)
        {
            string fullmsg = name + ": " + text;

            NetOutgoingMessage message = SS13NetServer.Singleton.CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)channel);
            message.Write(fullmsg);
            message.Write(entityId);

            SS13NetServer.Singleton.SendToAll(message);
        }

        /// <summary>
        /// Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">chat text</param>
        /// <param name="name">player name that sent the chat text</param>
        /// <param name="channel">channel message was recieved on</param>
        /// <param name="entityId">uid of the entity that sent the message. This will always be a player's attached entity</param>
        private void ProcessCommand(string text, string name, ChatChannel channel, int entityId, NetConnection client)
        {
            List<string> args = new List<string>();

            ParseArguments(text, args);

            string command = args[0];

            Vector2 position;
            Entity player;
            player = EntityManager.Singleton.GetEntity(entityId);
            if (player == null)
                position = new Vector2(160, 160);
            else
                position = player.position;

            switch (command)
            {
                case "spawnentity":
                    Entity spawned = EntityManager.Singleton.SpawnEntityAt(args[1], position);
                    break;
                case "crowbar":
                    EntityManager.Singleton.SpawnEntityAt("Crowbar", position);                   
                    break;
                case "toolbox":
                    EntityManager.Singleton.SpawnEntityAt("Toolbox", position);  
                    break;
                case "addgas":
                    if (args.Count > 1 && Convert.ToInt32(args[1]) > 0)
                    {
                        int amount = Convert.ToInt32(args[1]);
                        SS13Server.Singleton.Map.AddGasAt(SS13Server.Singleton.Map.GetTileArrayPositionFromWorldPosition(position), GasType.Toxin, amount);
                    }
                    break;
                case "gasreport":

                    var p = SS13Server.Singleton.Map.GetTileArrayPositionFromWorldPosition(position);
                    var c = SS13Server.Singleton.Map.GetTileAt(p.X, p.Y).gasCell;
                    foreach(var g in c.gasses)
                    {
                        SS13Server.Singleton.ChatManager.SendChatMessage(ChatChannel.Default, g.Key.ToString() + ": " + g.Value.ToString(), "GasReport", 0);
                    }
                    
                    break;
                case "sprayblood":
                    if (player == null)
                        return;
                    else
                        position = player.position;
                    p = SS13Server.Singleton.Map.GetTileArrayPositionFromWorldPosition(position);
                    var t = SS13Server.Singleton.Map.GetTileAt(p.X, p.Y);
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
                    SendChatMessage(channel, message, name, entityId);
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

        public ServerServiceType ServiceType
        {
            get { return ServerServiceType.ChatManager; }
        }
    }
}
