using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Commands;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Player;
using SS14.Server.Services.Chat.Commands;
using SS14.Server.Services.Log;
using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SS14.Server.Services.Chat
{
    public class ChatManager : IChatManager
    {
        private ISS14Server _serverMain;

        private Dictionary<string, Emote> _emotes = new Dictionary<string, Emote>();
        private Dictionary<string, IClientCommand> _commands = new Dictionary<string, IClientCommand>();
        private string _emotePath = @"emotes.xml";

        #region IChatManager Members

        public void Initialize(ISS14Server server)
        {
            _serverMain = server;
            LoadEmotes();
            LoadCommands();
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            //Read the chat message and pass it on
            var channel = (ChatChannel)message.ReadByte();
            string text = message.ReadString();
            IClient client = _serverMain.GetClient(message.SenderConnection);
            string name = client.PlayerName;
            LogManager.Log("CHAT- Channel " + channel.ToString() + " - Player " + name + "Message: " + text + "\n");
            var entityId = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(message.SenderConnection).AttachedEntityUid;

            bool hasChannelIdentifier = false;
            if (channel != ChatChannel.Lobby)
                channel = DetectChannel(text, out hasChannelIdentifier);
            if (hasChannelIdentifier)
                text = text.Substring(1);
            text = text.Trim(); // Remove whitespace
            if (text[0] == '/')
                ProcessCommand(text, name, channel, entityId, client);
            else if (text[0] == '*')
                ProcessEmote(text, name, channel, entityId, message.SenderConnection);
            else
                SendChatMessage(channel, text, name, entityId);
        }

        public void SendChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            NetOutgoingMessage message = MakeNetChatMessage(channel, text, name, entityId);

            switch (channel)
            {
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                case ChatChannel.Player:
                case ChatChannel.Default:
                    IoCManager.Resolve<ISS14NetServer>().SendToAll(message);
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

        public void SendPrivateMessage(IClient client, ChatChannel channel, string text, string name, int? entityId)
        {
            NetOutgoingMessage message = MakeNetChatMessage(channel, text, name, entityId);
            IoCManager.Resolve<ISS14NetServer>().SendMessage(message, client.NetConnection);
        }

        private NetOutgoingMessage MakeNetChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            string fullmsg = text;
            if (!string.IsNullOrEmpty(name) && channel == ChatChannel.Emote)
                fullmsg = text; //Emote already has name in it probably...
            else if (channel == ChatChannel.Ingame || channel == ChatChannel.OOC || channel == ChatChannel.Radio ||
                     channel == ChatChannel.Lobby)
                fullmsg = name + ": " + text;

            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();

            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)channel);
            message.Write(fullmsg);
            if (entityId == null)
                message.Write(-1);
            else
                message.Write((int)entityId);

            return message;
        }

        public Dictionary<string, IClientCommand> GetCommands()
        {
            return _commands;
        }

        #endregion IChatManager Members

        private ChatChannel DetectChannel(string message, out bool hasChannelIdentifier)
        {
            hasChannelIdentifier = false;
            var channel = ChatChannel.Ingame;
            switch (message[0])
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

        private void LoadEmotes()
        {
            if (File.Exists(_emotePath))
            {
                using (var emoteFileStream = new FileStream(_emotePath, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Emote>));

                    var emotes = (List<Emote>)serializer.Deserialize(emoteFileStream);
                    emoteFileStream.Close();
                    foreach (var emote in emotes)
                    {
                        _emotes.Add(emote.Command, emote);
                    }
                }
            }
            else
            {
                using (var emoteFileStream = new FileStream(_emotePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    var emote = new Emote();
                    emote.Command = "default";
                    emote.OtherText = "{0} does something!";
                    emote.SelfText = "You do something!";
                    _emotes.Add("default", emote);
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Emote>));
                    serializer.Serialize(emoteFileStream, _emotes.Values.ToList());
                    emoteFileStream.Close();
                }
            }
        }

        // Load all command types.
        private void LoadCommands()
        {
            List<Type> CommandTypes = new List<Type>();
            CommandTypes.AddRange(
                Assembly.GetCallingAssembly().GetTypes().Where(t => typeof(ChatCommand).IsAssignableFrom(t))
            );

            foreach (Type t in CommandTypes)
            {
                if (t == typeof(ChatCommand))
                    continue;
                ChatCommand instance = (ChatCommand)Activator.CreateInstance(t, null);
                instance.Register(_commands); // Done like this so scripted commands can be less hardcoded.
            }
        }

        private void SendToPlayersInRange(NetOutgoingMessage message, int? entityId)
        {
            if (entityId == null)
                return;
            List<NetConnection> recipients =
                IoCManager.Resolve<IPlayerManager>().GetPlayersInRange(
                    _serverMain.EntityManager.GetEntity((int)entityId).GetComponent<ITransformComponent>(
                        ComponentFamily.Transform).Position, 512).Select(p => p.ConnectedClient).ToList();
            IoCManager.Resolve<ISS14NetServer>().SendToMany(message, recipients);
        }

        private void SendToLobby(NetOutgoingMessage message)
        {
            List<NetConnection> recipients =
                IoCManager.Resolve<IPlayerManager>().GetPlayersInLobby().Select(p => p.ConnectedClient).ToList();
            IoCManager.Resolve<ISS14NetServer>().SendToMany(message, recipients);
        }

        private void ProcessEmote(string text, string name, ChatChannel channel, int? entityId, NetConnection client)
        {
            if (entityId == null)
                return; //No emotes from non-entities!

            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);
            if (_emotes.ContainsKey(args[0]))
            {
                var userText = String.Format(_emotes[args[0]].SelfText, name);//todo user-only channel
                var otherText = String.Format(_emotes[args[0]].OtherText, name, "his"); //todo pronouns, gender
                SendChatMessage(ChatChannel.Emote, otherText, name, entityId);
            }
            else
            {
                //todo Bitch at the user
            }
        }

        /// <summary>
        /// Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">Text content.</param>
        /// <param name="name">Player name that sent the chat text.</param>
        /// <param name="channel">Channel message was recieved on.</param>
        /// <param name="client">Client that sent the command.</param>
        private void ProcessCommand(string text, string name, ChatChannel channel, int? entityId, IClient client)
        {
            List<string> args = new List<string>();

            CommandParsing.ParseArguments(text.Substring(1), args); // Parse, but cut out the first character (/).

            if (args.Count <= 0)
                return;

            string command = args[0];
            if (!_commands.ContainsKey(command))
            {
                string message = string.Format("Command '{0}' not found.", command);
                SendPrivateMessage(client, ChatChannel.Default, message, "Server", null);
                return;
            }

            _commands[command].Execute(client, args.ToArray());
        }
    }

    public struct Emote
    {
        public string Command;
        public string SelfText;
        public string OtherText;
    }
}
