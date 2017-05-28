using Lidgren.Network;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;

namespace SS14.Client.State.States
{
    public class LobbyScreen : State, IState
    {
        private const double PlayerListRefreshDelaySec = 3; //Time in seconds before refreshing the playerlist.

        private readonly List<String> _playerListStrings = new List<string>();
        private string _gameType;
        private Chatbox _lobbyChat;
        private TextSprite _lobbyText;
        private DateTime _playerListTime;
        private string _serverMapName;
        private int _serverMaxPlayers;
        private string _serverName;
        private int _serverPort;
        private string _welcomeString;

        public LobbyScreen(IDictionary<Type, object> managers)
            : base(managers)
        {
        }

        #region IState Members

        public void Startup()
        {
            UserInterfaceManager.DisposeAllComponents();

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            _lobbyChat = new Chatbox("lobbychat", new Vector2i(475, 175), ResourceManager);
            _lobbyChat.TextSubmitted += LobbyChatTextSubmitted;

            _lobbyChat.Update(0);

            UserInterfaceManager.AddComponent(_lobbyChat);

            _lobbyText = new TextSprite("lobbyText", "", ResourceManager.GetFont("CALIBRI"))
            {
                Color = SFML.Graphics.Color.Black,
                ShadowColor = SFML.Graphics.Color.Transparent,
                Shadowed = true,
                //TODO CluwneSprite ShadowOffset
                // ShadowOffset = new Vector2(1, 1)
            };

            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName()); //Send name.

            NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
            playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
            NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

            _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);

            var joinButton = new Button("Join Game", ResourceManager) { mouseOverColor = new SFML.Graphics.Color(176, 222, 196) };
            joinButton.Position = new Vector2i(605 - joinButton.ClientArea.Width - 5,
                                            200 - joinButton.ClientArea.Height - 5);
            joinButton.Clicked += JoinButtonClicked;

            UserInterfaceManager.AddComponent(joinButton);

            CluwneLib.CurrentRenderTarget.Clear();
        }

        public void Render(FrameEventArgs e)
        {
            //public Vertex(Vector2f position, Color color, Vector2f texCoords);

            CluwneLib.CurrentRenderTarget.Clear();

            _lobbyText.Position = new Vector2i(10, 10);
            _lobbyText.Text = "Server: " + _serverName;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2i(10, 30);
            _lobbyText.Text = "Server-Port: " + _serverPort;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2i(10, 50);
            _lobbyText.Text = "Max Players: " + _serverMaxPlayers;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2i(10, 70);
            _lobbyText.Text = "Gamemode: " + _gameType;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2i(10, 110);
            _lobbyText.Text = "MOTD: \n" + _welcomeString;
            _lobbyText.Draw();

            int pos = 225;
            foreach (string plrStr in _playerListStrings)
            {
                _lobbyText.Position = new Vector2i(10, pos);
                _lobbyText.Text = plrStr;
                _lobbyText.Draw();
                pos += 20;
            }

            UserInterfaceManager.Render(e);
        }

        public void FormResize()
        {
            UserInterfaceManager.ResizeComponents();
        }

        public void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
            //TODO RenderTargetCache.DestroyAll();
        }

        public void Update(FrameEventArgs e)
        {
            UserInterfaceManager.Update(e);
            if (_playerListTime.CompareTo(DateTime.Now) < 0)
            {
                NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
                playerListMsg.Write((byte)NetMessage.PlayerList); // Request Playerlist.
                NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

                _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);
            }
        }

        #endregion IState Members

        private void JoinButtonClicked(Button sender)
        {
            PlayerManager.SendVerb("joingame", 0);
        }

        private void LobbyChatTextSubmitted(Chatbox chatbox, string text)
        {
            SendLobbyChat(text);
        }

        private void NetworkManagerMessageArrived(object sender, IncomingNetworkMessageArgs args)
        {
            NetIncomingMessage message = args.Message;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus)message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        string disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager,
                                                                                        UserInterfaceManager,
                                                                                        ResourceManager,
                                                                                        disconnectMessage));
                    }
                    break;

                case NetIncomingMessageType.Data:
                    var messageType = (NetMessage)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.LobbyChat:
                            string text = message.ReadString();
                            AddChat(text);
                            break;

                        case NetMessage.PlayerCount:
                            //TODO var newCount = message.ReadByte();
                            break;

                        case NetMessage.PlayerList:
                            HandlePlayerList(message);
                            break;

                        case NetMessage.WelcomeMessage:
                            HandleWelcomeMessage(message);
                            break;

                        case NetMessage.ChatMessage:
                            HandleChatMessage(message);
                            break;

                        case NetMessage.JoinGame:
                            HandleJoinGame();
                            break;
                    }
                    break;
            }
        }

        private void HandlePlayerList(NetIncomingMessage msg)
        {
            byte playerCount = msg.ReadByte();
            _playerListStrings.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                string currName = msg.ReadString();
                var currStatus = (SessionStatus)msg.ReadByte();
                float currRoundtrip = msg.ReadFloat();
                _playerListStrings.Add(currName + "\t\tStatus: " + currStatus + "\t\tLatency: " +
                                       Math.Truncate(currRoundtrip * 1000) + " ms");
            }
        }

        private void HandleJoinGame()
        {
            StateManager.RequestStateChange<GameScreen>();
        }

        private void AddChat(string text)
        {
            _lobbyChat.AddLine(text, ChatChannel.Lobby);
        }

        public void SendLobbyChat(string text)
        {
            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Lobby);
            message.Write(text);

            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            _serverName = msg.ReadString();
            _serverPort = msg.ReadInt32();
            _welcomeString = msg.ReadString();
            _serverMaxPlayers = msg.ReadInt32();
            _serverMapName = msg.ReadString();
            _gameType = msg.ReadString();
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();
            string message = "[" + channel + "] " + text;
            _lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        #region Input

        public void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp(KeyEventArgs e)
        {
        }

        public void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMoved(MouseMoveEventArgs e)
        {
        }

        public void MousePressed(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMove(MouseMoveEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove(MouseWheelEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }

        public void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        public void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e);
        }

        #endregion Input
    }
}
