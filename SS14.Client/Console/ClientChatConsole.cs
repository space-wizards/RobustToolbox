using System.Collections.Generic;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;
using SS14.Shared.Log;

namespace SS14.Client.Console
{
    /// <summary>
    ///     Expands the console to support chat, channels, and emotes.
    /// </summary>
    public class ClientChatConsole : ClientConsole, IClientChatConsole
    {
        private const char ConCmdSlash = '/';
        private const char OocAlias = '[';
        private const char MeAlias = '@';

        private readonly Dictionary<ChatChannel, Color> _chatColors;

        [Dependency]
        private readonly IClientEntityManager _entityManager;

        [Dependency]
        private readonly IPlayerManager _players;

        /// <summary>
        ///     Default Constructor.
        /// </summary>
        public ClientChatConsole()
        {
            _chatColors = new Dictionary<ChatChannel, Color>
            {
                [ChatChannel.Default] = Color.Gray,
                [ChatChannel.Damage] = Color.Red,
                [ChatChannel.Radio] = new Color(0, 100, 0),
                [ChatChannel.Server] = Color.Blue,
                [ChatChannel.Player] = new Color(0, 128, 0),
                [ChatChannel.Local] = new Color(0, 200, 0),
                [ChatChannel.OOC] = Color.White,
                [ChatChannel.Emote] = Color.Cyan,
                [ChatChannel.Visual] = Color.Yellow,
            };
        }

        /// <summary>
        ///     Initializes the console into a useable state.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _network.RegisterNetMessage<MsgChat>(MsgChat.NAME, msg => HandleChatMsg((MsgChat) msg));
        }

        /// <inheritdoc />
        public void ParseChatMessage(Chatbox chatBox, string text)
        {
            ParseChatMessage(text, chatBox.DefaultChatFormat);
        }

        /// <inheritdoc />
        public void ParseChatMessage(string text, string defaultFormat = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            switch (text[0])
            {
                case ConCmdSlash:
                    {
                        // run locally
                        var conInput = text.Substring(1);
                        ProcessCommand(conInput);
                        break;
                    }
                case OocAlias:
                    {
                        var conInput = text.Substring(2);
                        ProcessCommand($"ooc \"{conInput}\"");
                        break;
                    }
                case MeAlias:
                    {
                        var conInput = text.Substring(2);
                        ProcessCommand($"me \"{conInput}\"");
                        break;
                    }
                default:
                    {
                        var conInput = defaultFormat != null ? string.Format(defaultFormat, text) : text;
                        Logger.Debug(conInput);
                        ProcessCommand(conInput);
                        break;
                    }
            }
        }

        private void HandleChatMsg(MsgChat msg)
        {
            Logger.Debug($"Got it! {msg.Text}");
            var channel = msg.Channel;
            var text = msg.Text;
            var index = msg.Index;
            var entityId = msg.EntityId;

            switch (channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                    {
                        string name;
                        if (index.HasValue && _players.SessionsDict.TryGetValue(index.Value, out var session))
                        {
                            name = session.Name;
                        }
                        else if (entityId.HasValue)
                        {
                            var ent = _entityManager.GetEntity(entityId.Value);
                            name = ent.Name ?? ent.ToString();
                        }
                        else
                        {
                            name = "<TERU-SAMA>";
                        }

                        text = $"[{channel}] {name}: {text}";
                        break;
                    }
            }

            AddLine(text, channel, GetChannelColor(channel));

            if (entityId.HasValue && _entityManager.TryGetEntity(entityId.Value, out var a))
                a.SendMessage(null, new SaidSomethingMsg(channel, text));
        }

        private Color GetChannelColor(ChatChannel channel)
        {
            if (_chatColors.TryGetValue(channel, out var color))
                return color;

            return Color.White;
        }
    }
}
