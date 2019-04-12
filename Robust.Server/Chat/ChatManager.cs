using System.Collections.Generic;
using System.Xml.Serialization;
using Robust.Server.Interfaces.Chat;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;
using Robust.Shared.Interfaces;
using Robust.Shared.Players;

namespace Robust.Server.Chat
{
    /// <summary>
    ///     Dispatches chat messages to clients.
    /// </summary>
    public class ChatManager : IChatManager
    {
        private const string DefaultPronoun = "their";

        [Dependency]
        private readonly IServerNetManager _network;

        [Dependency]
        private readonly IResourceManager _resources;

        private readonly Dictionary<string, Emote> _emotes = new Dictionary<string, Emote>();

        /// <inheritdoc />
        public void Initialize()
        {
            _network.RegisterNetMessage<MsgChat>(MsgChat.NAME);
            LoadEmotes();
        }

        /// <inheritdoc />
        public void DispatchMessage(INetChannel client, ChatChannel channel, string text, NetSessionId? index = null, EntityUid? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendMessage(msg, client);
        }

        /// <inheritdoc />
        public void DispatchMessage(List<INetChannel> clients, ChatChannel channel, string text, NetSessionId? index = null, EntityUid? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendToMany(msg, clients);
        }

        /// <inheritdoc />
        public void DispatchMessage(ChatChannel channel, string text, NetSessionId? index = null, EntityUid? entityUid = null)
        {
            var msg = BuildChatMessage(channel, text, index, entityUid);
            _network.ServerSendToAll(msg);
        }

        /// <inheritdoc />
        public bool ExpandEmote(string input, IPlayerSession session, out string self, out string other)
        {
            if (_emotes.TryGetValue(input, out var emote))
            {
                //TODO: Notify content, allow it to override expansion
                // args: session, Emote

                self = string.Format(emote.SelfText);
                other = string.Format(emote.OtherText, session.Name, DefaultPronoun);
                return true;
            }

            self = string.Empty;
            other = string.Empty;
            return false;
        }

        private MsgChat BuildChatMessage(ChatChannel channel, string text, NetSessionId? index, EntityUid? entityUid)
        {
            var message = _network.CreateNetMessage<MsgChat>();

            message.Channel = channel;
            message.Text = text;
            message.SessionId = index;
            message.EntityId = entityUid;

            return message;
        }

        private void LoadEmotes()
        {
            if (!_resources.TryContentFileRead(new ResourcePath(@"/emotes.xml"), out var emoteFileStream))
                return;

            var serializer = new XmlSerializer(typeof(List<Emote>));
            var emotes = (List<Emote>)serializer.Deserialize(emoteFileStream);
            emoteFileStream.Close();

            foreach (var emote in emotes)
            {
                _emotes.Add(emote.Command, emote);
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
