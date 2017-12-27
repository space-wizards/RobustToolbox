using SS14.Client.Interfaces.GameObjects;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;

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

        [Dependency]
        private IClientEntityManager _entityManager;

        /// <summary>
        ///     Initializes the console into a useable state.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _network.RegisterNetMessage<MsgChat>(MsgChat.NAME, (int) MsgChat.ID, msg => HandleChatMsg((MsgChat) msg));
        }

        /// <inheritdoc />
        public void ParseChatMessage(Chatbox chatBox, string text)
        {
            ParseChatMessage(text, chatBox.DefaultChatFormat);
        }

        /// <inheritdoc />
        public void ParseChatMessage(string text, string defaultFormat = null)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
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
                    ProcessCommand($"ooc {conInput}");
                    break;
                }
                case MeAlias:
                {
                    var conInput = text.Substring(2);
                    ProcessCommand($"me {conInput}");
                    break;
                }
                default:
                {
                    var conInput = defaultFormat != null ? string.Format(defaultFormat, text) : text;
                    ProcessCommand(conInput);
                    break;
                }
            }
        }

        private void HandleChatMsg(MsgChat msg)
        {
            var channel = msg.Channel;
            var text = msg.Text;
            var entityId = msg.EntityId;

            switch (channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                    text = "[" + channel + "] " + text;
                    break;
            }

            AddLine(text, channel, GetChannelColor(channel));

            if (entityId.HasValue && _entityManager.TryGetEntity(entityId.Value, out var a))
                a.SendMessage(this, ComponentMessageType.EntitySaidSomething, channel, text);
        }

        private Color GetChannelColor(ChatChannel channel)
        {
            //TODO: Actually implement this.
            return Color.Yellow;
        }
    }
}
