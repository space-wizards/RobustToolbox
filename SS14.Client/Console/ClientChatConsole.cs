using Lidgren.Network;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared;
using SS14.Shared.Console;
using SS14.Shared.GameObjects;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Console
{
    /// <summary>
    ///     Expands the console to support chat, channels, and emotes.
    /// </summary>
    public class ClientChatConsole : ClientConsole, IClientChatConsole
    {
        private const char ConCmdSlash = '/';

        [Dependency]
        private IClientEntityManager _entityManager;

        /// <summary>
        ///     Initializes the console into a useable state.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _network.RegisterNetMessage<MsgChat>(MsgChat.NAME, (int) MsgChat.ID, HandleChatMsg);
        }

        /// <inheritdoc />
        public void ParseChatMessage(Chatbox chatBox, string text)
        {
            ParseChatMessage(text);
        }

        /// <inheritdoc />
        public void ParseChatMessage(string text)
        {
            //TODO: Actually parse the message
            if(string.IsNullOrWhiteSpace(text))
                return;

            // detect '/' concmd
            if (text[0] == ConCmdSlash)
            {
                // run locally
                var conInput = text.Substring(1);
                ProcessCommand(conInput);
            }

            // say
            var message = _network.CreateNetMessage<MsgChat>();
            message.Channel = ChatChannel.Player;
            message.Text = text;
            message.EntityId = -1;
            _network.ClientSendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void HandleChatMsg(NetMessage message)
        {
            var msg = (MsgChat) message;

            var channel = msg.Channel;
            var text = msg.Text;
            var entityId = msg.EntityId;

            switch (channel)
            {
                case ChatChannel.Ingame:
                case ChatChannel.Server:
                case ChatChannel.OOC:
                case ChatChannel.Radio:
                    text = "[" + channel + "] " + text;
                    break;
            }
            
            AddLine(text, channel, GetChannelColor(channel));
            if (entityId.HasValue && _entityManager.TryGetEntity(entityId.Value, out var a))
            {
                a.SendMessage(this, ComponentMessageType.EntitySaidSomething, channel, text);
            }
        }

        private Color GetChannelColor(ChatChannel channel)
        {
            //TODO: Actually implement this.
            return Color.Yellow;
        }
    }
}
