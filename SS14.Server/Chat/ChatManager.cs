using OpenTK;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Reflection;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Chat
{
    public class ChatManager : IChatManager
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        [Dependency]
        private readonly IServerEntityManager entityManager;

        private readonly Dictionary<string, Emote> _emotes = new Dictionary<string, Emote>();
        private readonly Dictionary<string, IChatCommand> _commands = new Dictionary<string, IChatCommand>();
        private readonly string _emotePath = PathHelpers.ExecutableRelativeFile("emotes.xml");

        public IDictionary<string, IChatCommand> Commands => _commands;

        #region IChatManager Members

        public void Initialize()
        {
            LoadEmotes();
            LoadCommands();
        }

        public void HandleNetMessage(MsgChat message)
        {
            var channel = message.Channel;
            string text = message.Text;

            var client = message.MsgChannel;

            var session = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(message.MsgChannel);
            var playerName = session.Name;

            Logger.Debug("CHAT:: Channel: {0} :: Player: {1} :: Message: {2}", channel, playerName, text);

            var entityId = IoCManager.Resolve<IPlayerManager>().GetSessionByChannel(message.MsgChannel).AttachedEntityUid;

            bool hasChannelIdentifier = false;
            if (channel != ChatChannel.Lobby)
                channel = DetectChannel(text, out hasChannelIdentifier);
            if (hasChannelIdentifier)
                text = text.Substring(1);
            text = text.Trim(); // Remove whitespace

            if (text[0] == '/')
                ProcessCommand(text, playerName, channel, entityId, client);
            else if (text[0] == '*')
                ProcessEmote(text, playerName, channel, entityId, message.MsgChannel);
            else
                SendChatMessage(channel, text, playerName, entityId);
        }

        public void SendChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            MsgChat message = MakeNetChatMessage(channel, text, name, entityId);

            switch (channel)
            {
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                case ChatChannel.Player:
                case ChatChannel.Default:
                    IoCManager.Resolve<IServerNetManager>().ServerSendToAll(message);
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

        public void SendPrivateMessage(INetChannel client, ChatChannel channel, string text, string name, int? entityId)
        {
            MsgChat message = MakeNetChatMessage(channel, text, name, entityId);
            IoCManager.Resolve<IServerNetManager>().ServerSendMessage(message, client);
        }

        private MsgChat MakeNetChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            string fullmsg = text;
            if (!string.IsNullOrEmpty(name) && channel == ChatChannel.Emote)
                fullmsg = text; //Emote already has name in it probably...
            else if (channel == ChatChannel.Ingame || channel == ChatChannel.OOC || channel == ChatChannel.Radio ||
                     channel == ChatChannel.Lobby)
                fullmsg = name + ": " + text;

            MsgChat message = IoCManager.Resolve<IServerNetManager>().CreateNetMessage<MsgChat>();

            message.Channel = channel;
            message.Text = fullmsg;
            message.EntityId = entityId;

            return message;
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
                    var emote = new Emote()
                    {
                        Command = "default",
                        OtherText = "{0} does something!",
                        SelfText = "You do something!"
                    };
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
            foreach (Type t in reflectionManager.GetAllChildren<IChatCommand>())
            {
                IChatCommand instance = (IChatCommand)Activator.CreateInstance(t, null);
                if (_commands.ContainsKey(instance.Command))
                {
                    Logger.Error("Command has duplicate name: {0}", instance.Command);
                    continue;
                }
                _commands[instance.Command] = instance;
            }
        }

        private void SendToPlayersInRange(NetMessage message, int? entityId)
        {
            //TODO: Move this to a real PVS system.
            int withinRange = 512;
            if (entityId == null)
                return;
            List<INetChannel> recipients = IoCManager.Resolve<IPlayerManager>()
                .GetPlayersInRange(entityManager.GetEntity((int) entityId)
                .GetComponent<ITransformComponent>().LocalPosition, withinRange)
                .Select(p => p.ConnectedClient).ToList();

            IoCManager.Resolve<IServerNetManager>().ServerSendToMany(message, recipients);
        }

        private void SendToLobby(NetMessage message)
        {
            //TODO: Move this to the Content Assembly.
            List<INetChannel> recipients = IoCManager.Resolve<IPlayerManager>().GetPlayersInLobby().Select(p => p.ConnectedClient).ToList();
            IoCManager.Resolve<IServerNetManager>().ServerSendToMany(message, recipients);
        }

        private void ProcessEmote(string text, string name, ChatChannel channel, int? entityId, INetChannel client)
        {
            if (entityId == null)
                return; //No emotes from non-entities!

            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);
            if (_emotes.ContainsKey(args[0]))
            {
                // todo make a user-only channel that only the sender can see i.e. for emotes and game feedback ('you put the coins in the jar' or whatever)
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
        /// <param name="channel">Channel message was received on.</param>
        /// <param name="client">Client that sent the command.</param>
        private void ProcessCommand(string text, string name, ChatChannel channel, int? entityId, INetChannel client)
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

            _commands[command].Execute(this, client, args.ToArray());
        }
    }

    public struct Emote
    {
        public string Command { get; set; }
        public string SelfText { get; set; }
        public string OtherText { get; set; }
    }
}
