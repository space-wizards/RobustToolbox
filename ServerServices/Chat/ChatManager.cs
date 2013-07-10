using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using ServerInterfaces;
using ServerInterfaces.Chat;
using ServerInterfaces.GameObject;
using ServerInterfaces.Network;
using ServerServices.Log;
using SS13.IoC;
using SS13_Shared;
using ServerInterfaces.Player;
using ServerInterfaces.Map;
using ServerServices.Tiles;

namespace ServerServices.Chat
{
    public class ChatManager : IChatManager
    {
        private ISS13Server _serverMain;

        public void Initialize(ISS13Server server)
        {
            _serverMain = server;
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            ChatChannel channel = (ChatChannel)message.ReadByte();
            string text = message.ReadString();
            string name = _serverMain.GetClient(message.SenderConnection).PlayerName;
            LogManager.Log("CHAT- Channel " + channel.ToString() +  " - Player " + name + "Message: " + text + "\n");

            int entityId = 0;
            if (IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(message.SenderConnection).attachedEntity != null)
                entityId = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(message.SenderConnection).attachedEntity.Uid;

            bool hasChannelIdentifier = false;
            if(channel != ChatChannel.Lobby)
                channel = DetectChannel(text, out hasChannelIdentifier);
            if (hasChannelIdentifier)
                text = text.Substring(1);
            text = text.Trim(); // Remove whitespace
            if (text[0] == '/')
                ProcessCommand(text, name, channel, entityId, message.SenderConnection);
            else
                SendChatMessage(channel, text, name, entityId);
        }

        private ChatChannel DetectChannel(string message, out bool hasChannelIdentifier)
        {
            hasChannelIdentifier = false;
            var channel = ChatChannel.Ingame;
            switch(message[0])
            {
                case '[':
                    channel = ChatChannel.OOC;
                    hasChannelIdentifier = true;
                    break;
                case ':':
                    channel = ChatChannel.Radio;
                    hasChannelIdentifier = true;
                    break;
                case '@':
                    channel = ChatChannel.Emote;
                    hasChannelIdentifier = true;
                    break;
            }
            return channel;
        }

        public void SendChatMessage(ChatChannel channel, string text, string name, int entityId)
        {
            string fullmsg = text;
            if (!string.IsNullOrEmpty(name) && channel == ChatChannel.Emote)
                fullmsg = name + " " + text;
            else if (channel == ChatChannel.Ingame || channel == ChatChannel.OOC || channel == ChatChannel.Radio || channel == ChatChannel.Lobby)
                fullmsg = name + ": " + text;

            NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)channel);
            message.Write(fullmsg);
            message.Write(entityId);

            switch(channel)
            {
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                case ChatChannel.Player:
                case ChatChannel.Default:
                    IoCManager.Resolve<ISS13NetServer>().SendToAll(message);
                    break;
                case ChatChannel.Damage:
                case ChatChannel.Ingame:
                case ChatChannel.Visual:
                case ChatChannel.Emote:
                    SendToPlayersInRange(message, entityId);
                    break;
                case ChatChannel.Lobby:
                    SendToLobby(message);
                    break;
            }

        }

        private void SendToPlayersInRange(NetOutgoingMessage message, int entityId)
        {
            var recipients = IoCManager.Resolve<IPlayerManager>().GetPlayersInRange(_serverMain.EntityManager.GetEntity(entityId).Position, 512).Select(p => p.ConnectedClient).ToList();
            IoCManager.Resolve<ISS13NetServer>().SendToMany(message, recipients);
        }

        private void SendToLobby(NetOutgoingMessage message)
        {
            var recipients = IoCManager.Resolve<IPlayerManager>().GetPlayersInLobby().Select(p => p.ConnectedClient).ToList();
            IoCManager.Resolve<ISS13NetServer>().SendToMany(message, recipients);
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
            IEntity player;
            player = _serverMain.EntityManager.GetEntity(entityId);
            if (player == null)
                position = new Vector2(160, 160);
            else
                position = player.Position;
            
            var map = IoCManager.Resolve<IMapManager>();
            switch (command)
            {
                /*case "spawnentity":
                    Entity spawned = EntityManager.Singleton.SpawnEntityAt(args[1], position);
                    break;
                case "crowbar":
                    EntityManager.Singleton.SpawnEntityAt("Crowbar", position);                   
                    break;
                case "toolbox":
                    EntityManager.Singleton.SpawnEntityAt("Toolbox", position);  
                    break;*/
                case "addgas":
                    if (args.Count > 1 && Convert.ToInt32(args[1]) > 0)
                    {
                        int amount = Convert.ToInt32(args[1]);
                        
                        map.AddGasAt(map.GetTileArrayPositionFromWorldPosition(position), GasType.Toxin, amount);
                    }
                    break;
                case "gasreport":

                    var p = map.GetTileArrayPositionFromWorldPosition(position);
                    var t = (Tile) map.GetTileAt(p.X, p.Y);
                    var c = t.gasCell;
                    foreach (var g in c.GasMixture.gasses)
                    {
                        SendChatMessage(ChatChannel.Default, g.Key.ToString() + ": " + g.Value.ToString(), "GasReport", 0);
                    }
                    
                    break;
                case "everyonesondrugs":
                    foreach (var playerfordrugs in IoCManager.Resolve<IPlayerManager>().GetAllPlayers())
                    {
                        playerfordrugs.AddPostProcessingEffect(PostProcessingEffectType.Acid, 60);
                    }
                    break;
                /*    
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
                        
                    break;*/
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
    }
}
