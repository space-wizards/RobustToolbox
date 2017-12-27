using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace SS14.Server.Chat
{
    public class ChatManager : IChatManager
    {
        [Dependency]
        private readonly IServerEntityManager _entities;

        [Dependency]
        private readonly IServerNetManager _network;

        [Dependency]
        private readonly IPlayerManager _players;

        private readonly Dictionary<string, Emote> _emotes = new Dictionary<string, Emote>();
        private readonly string _emotePath = PathHelpers.ExecutableRelativeFile("emotes.xml");

        /// <inheritdoc />
        public void Initialize()
        {
            _network.RegisterNetMessage<MsgChat>(MsgChat.NAME, (int) MsgChat.ID);

            LoadEmotes();
        }

        /// <inheritdoc />
        public void DispatchMessage(INetChannel client, ChatChannel channel, string text, PlayerIndex? index = null, int? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendMessage(msg, client);
        }

        /// <inheritdoc />
        public void DispatchMessage(List<INetChannel> clients, ChatChannel channel, string text, PlayerIndex? index = null, int? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendToMany(msg, clients);
        }

        /// <inheritdoc />
        public void DispatchMessage(ChatChannel channel, string text, PlayerIndex? index = null, int? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendToAll(msg);
        }

        private MsgChat BuildChatMessage(ChatChannel channel, string text, PlayerIndex? index, int? entityUid)
        {
            var message = _network.CreateNetMessage<MsgChat>();

            message.Channel = channel;
            message.Text = text;
            message.Index = index;
            message.EntityId = entityUid;

            return message;
        }

        private void SendChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            var message = MakeNetChatMessage(channel, text, name, entityId);

            switch (channel)
            {
                case ChatChannel.Damage:
                case ChatChannel.Local:
                case ChatChannel.Visual:
                case ChatChannel.Emote:
                    SendToPlayersInRange(message, entityId);
                    break;

                default:
                    _network.ServerSendToAll(message);
                    break;
            }
        }

        private MsgChat MakeNetChatMessage(ChatChannel channel, string text, string name, int? entityId)
        {
            var fullmsg = text;
            if (!string.IsNullOrEmpty(name) && channel == ChatChannel.Emote)
                fullmsg = text; //Emote already has name in it probably...
            else if (channel == ChatChannel.Local || channel == ChatChannel.OOC || channel == ChatChannel.Radio)
                fullmsg = name + ": " + text;

            var message = _network.CreateNetMessage<MsgChat>();

            message.Channel = channel;
            message.Text = fullmsg;
            message.EntityId = entityId;

            return message;
        }

        private void LoadEmotes()
        {
            if (File.Exists(_emotePath))
                using (var emoteFileStream = new FileStream(_emotePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new XmlSerializer(typeof(List<Emote>));

                    var emotes = (List<Emote>) serializer.Deserialize(emoteFileStream);
                    emoteFileStream.Close();
                    foreach (var emote in emotes)
                    {
                        _emotes.Add(emote.Command, emote);
                    }
                }
            else
                using (var emoteFileStream = new FileStream(_emotePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    var emote = new Emote
                    {
                        Command = "default",
                        OtherText = "{0} does something!",
                        SelfText = "You do something!"
                    };
                    _emotes.Add("default", emote);
                    var serializer = new XmlSerializer(typeof(List<Emote>));
                    serializer.Serialize(emoteFileStream, _emotes.Values.ToList());
                    emoteFileStream.Close();
                }
        }

        private void SendToPlayersInRange(NetMessage message, int? entityId)
        {
            //TODO: Move this to a real PVS system.
            var withinRange = 512;
            if (entityId == null)
                return;
            var recipients = _players
                .GetPlayersInRange(_entities.GetEntity((int) entityId)
                    .GetComponent<ITransformComponent>().LocalPosition, withinRange)
                .Select(p => p.ConnectedClient).ToList();

            _network.ServerSendToMany(message, recipients);
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
                var otherText = string.Format(_emotes[args[0]].OtherText, name, "his"); //todo pronouns, gender
                SendChatMessage(ChatChannel.Emote, otherText, name, entityId);
            }
        }

        // xml serializer requires this to be public
        public struct Emote
        {
            public string Command { get; set; }
            public string SelfText { get; set; }
            public string OtherText { get; set; }
        }
    }
}
