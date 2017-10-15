using Lidgren.Network;
using OpenTK;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Network;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.State.States
{
    public class Lobby : State, IState
    {
        #region Fields

        private readonly Sprite _background;

        private readonly SimpleImage _imgStatus;

        private readonly Label _lblMode;
        private readonly Label _lblModeInfo;

        private readonly Label _lblPlayers;
        private readonly Label _lblPlayersInfo;

        private readonly Label _lblPort;
        private readonly Label _lblPortInfo;
        private readonly Label _lblServer;
        private readonly Label _lblServerInfo;
        private readonly SimpleImage _imgMainBg;
        private SimpleImage _imgChatBg;
        private ImageButton _btnReady;
        private ImageButton _btnBack;

        private readonly List<Label> _serverLabels = new List<Label>();

        //Keep track of previous tick screen width and height for use in update.
        private int _prevScreenWidth = 0;

        private int _prevScreenHeight = 0;

        private readonly TabContainer _tabCharacter;
        private readonly TabContainer _tabObserve;
        private readonly PlayerListTab _tabServer;
        private readonly TabbedMenu _tabs;

        private float _lastLblSpacing = 10;
        //TODO Actually calculate this and adjust all labels accordingly. Make sure we compensate if labels longer than status line.

        private Box2 _recStatus;

        private Chatbox _lobbyChat;

        private string _serverName;
        private int _serverPort;
        private string _welcomeString;
        private int _serverMaxPlayers;
        private int _serverPlayers;
        private string _serverMapName;
        private string _gameType;

        #endregion Fields

        public Lobby(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceCache.GetSprite("mainbg");
            _background.Texture.Smooth = true;

            _imgMainBg = new SimpleImage
            {
                Sprite = "lobby_mainbg"
            };

            _imgStatus = new SimpleImage
            {
                Sprite = "lobby_statusbar"
            };

            _lblServer = new Label("SERVER:", "MICROGME", ResourceCache);
            _lblServer.Text.Color = new Color4(245, 245, 245, 255);
            _serverLabels.Add(_lblServer);

            _lblServerInfo = new Label("LLJK#1", "MICROGME", ResourceCache);
            _lblServerInfo.Text.Color = new Color4(139, 0, 0, 255);
            _serverLabels.Add(_lblServerInfo);

            _lblMode = new Label("GAMEMODE:", "MICROGME", ResourceCache);
            _lblMode.Text.Color = new Color4(245, 245, 245, 255);
            _serverLabels.Add(_lblMode);

            _lblModeInfo = new Label("SECRET", "MICROGME", ResourceCache);
            _lblModeInfo.Text.Color = new Color4(139, 0, 0, 255);
            _serverLabels.Add(_lblModeInfo);

            _lblPlayers = new Label("PLAYERS:", "MICROGME", ResourceCache);
            _lblPlayers.Text.Color = new Color4(245, 245, 245, 255);
            _serverLabels.Add(_lblPlayers);

            _lblPlayersInfo = new Label("17/32", "MICROGME", ResourceCache);
            _lblPlayersInfo.Text.Color = new Color4(139, 0, 0, 255);
            _serverLabels.Add(_lblPlayersInfo);

            _lblPort = new Label("PORT:", "MICROGME", ResourceCache);
            _lblPort.Text.Color = new Color4(245, 245, 245, 255);
            _serverLabels.Add(_lblPort);

            _lblPortInfo = new Label(MainScreen.DEFAULT_PORT.ToString(), "MICROGME", ResourceCache);
            _lblPortInfo.Text.Color = new Color4(139, 0, 0, 255);
            _serverLabels.Add(_lblPortInfo);

            _tabs = new TabbedMenu
            {
                TopSprite = "lobby_tab_top",
                MidSprite = "lobby_tab_mid",
                BotSprite = "lobby_tab_bot",
                TabOffset = new Vector2i(-8, 300),
                ZDepth = 2
            };

            _tabCharacter = new TabContainer("lobbyTabCharacter", new Vector2i(793, 450), ResourceCache)
            {
                tabSpriteName = "lobby_tab_person"
            };
            _tabs.AddTab(_tabCharacter);

            _tabObserve = new TabContainer("lobbyTabObserve", new Vector2i(793, 450), ResourceCache)
            {
                tabSpriteName = "lobby_tab_eye"
            };
            _tabs.AddTab(_tabObserve);

            _tabServer = new PlayerListTab("lobbyTabServer", new Vector2i(793, 450), ResourceCache)
            {
                tabSpriteName = "lobby_tab_info"
            };
            _tabs.AddTab(_tabServer);
            _tabs.SelectTab(_tabServer);

            _lobbyChat = new Chatbox("lobbychat", new Vector2i(780, 225), ResourceCache);
            _lobbyChat.Update(0);

            _imgChatBg = new SimpleImage()
            {
                Sprite = "lobby_chatbg",
            };

            _lobbyChat.TextSubmitted += new Chatbox.TextSubmitHandler(_lobbyChat_TextSubmitted);

            _btnReady = new ImageButton()
            {
                ImageNormal = "lobby_ready",
                ImageHover = "lobby_ready_green",
                ZDepth = 1
            };
            _btnReady.Clicked += _btnReady_Clicked;
            _btnReady.Update(0);

            _btnBack = new ImageButton()
            {
                ImageNormal = "lobby_back",
                ImageHover = "lobby_back_green",
                ZDepth = 1
            };
            _btnBack.Clicked += _btnBack_Clicked;
            _btnBack.Update(0);

            _lblServerInfo.FixedWidth = 100;
            _lblModeInfo.FixedWidth = 90;
            _lblPlayersInfo.FixedWidth = 60;
            _lblPortInfo.FixedWidth = 50;

            UpdateGUIPosition();
        }

        private void _lobbyChat_TextSubmitted(Chatbox chatbox, string text)
        {
            throw new NotImplementedException();
        }

        #region Network

        private void NetworkManagerMessageArrived(object sender, NetMessageArgs args)
        {
            NetIncomingMessage message = args.RawMessage;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus)message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        string disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager,
                                                                                        UserInterfaceManager,
                                                                                        ResourceCache,
                                                                                        disconnectMessage));
                    }
                    break;

                case NetIncomingMessageType.Data:
                    var messageType = (NetMessages)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessages.LobbyChat:
                            //TODO: Send player messages to a lobby chat
                            break;

                        case NetMessages.PlayerList:
                            HandlePlayerList(message);
                            break;

                        case NetMessages.WelcomeMessage:
                            HandleWelcomeMessage(message);
                            break;

                        case NetMessages.ChatMessage:
                            HandleChatMessage(message);
                            break;

                        case NetMessages.JoinGame:
                            HandleJoinGame();
                            break;
                    }
                    break;
            }
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

        private void HandlePlayerList(NetIncomingMessage message)
        {
            byte playerCount = message.ReadByte();
            _serverPlayers = playerCount;
            _tabServer._scPlayerList.components.Clear();
            int offY = 0;
            for (int i = 0; i < playerCount; i++)
            {
                string currName = message.ReadString();
                var currStatus = (SessionStatus)message.ReadByte();
                float currRoundtrip = message.ReadFloat();

                Label newLabel = new Label(currName + "\t\tStatus: " + currStatus + "\t\tLatency: " + Math.Truncate(currRoundtrip * 1000) + " ms", "MICROGBE", ResourceCache);
                newLabel.Position = new Vector2i(0, offY);
                newLabel.TextColor = Color4.Black;
                newLabel.Update(0);
                offY += newLabel.ClientArea.Height;
                _tabServer._scPlayerList.components.Add(newLabel);
            }
        }

        private void HandleJoinGame()
        {
            StateManager.RequestStateChange<GameScreen>();
        }

        #endregion Network

        #region Startup, Shutdown, Update

        public void Startup()
        {
            UserInterfaceManager.AddComponent(_imgMainBg);
            UserInterfaceManager.AddComponent(_imgStatus);
            UserInterfaceManager.AddComponent(_tabs);
            UserInterfaceManager.AddComponent(_imgChatBg);
            UserInterfaceManager.AddComponent(_lobbyChat);
            UserInterfaceManager.AddComponent(_btnReady);
            UserInterfaceManager.AddComponent(_btnBack);

            foreach (Label curr in _serverLabels)
                UserInterfaceManager.AddComponent(curr);

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessages.WelcomeMessageReq); //Request Welcome msg.
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableOrdered);

            NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
            playerListMsg.Write((byte)NetMessages.PlayerListReq); //Request Playerlist.
            NetworkManager.ClientSendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_imgMainBg);
            UserInterfaceManager.RemoveComponent(_imgStatus);
            UserInterfaceManager.RemoveComponent(_tabs);
            UserInterfaceManager.RemoveComponent(_imgChatBg);
            UserInterfaceManager.RemoveComponent(_lobbyChat);
            UserInterfaceManager.RemoveComponent(_btnReady);
            UserInterfaceManager.RemoveComponent(_btnBack);

            foreach (Label curr in _serverLabels)
                UserInterfaceManager.RemoveComponent(curr);

            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
        }

        public void Update(FrameEventArgs e)
        {
            if (CluwneLib.Window.Viewport.Size.X != _prevScreenWidth || CluwneLib.Window.Viewport.Size.Y != _prevScreenHeight)
            {
                _prevScreenHeight = (int)CluwneLib.Window.Viewport.Size.Y;
                _prevScreenWidth = (int)CluwneLib.Window.Viewport.Size.X;
                UpdateGUIPosition();
            }

            // This might be a hacky solution, but the button loses focus way too fast.
            _btnReady.Focus = true;

            _lblServerInfo.Text.Text = _serverName;
            _lblModeInfo.Text.Text = _gameType;
            _lblPlayersInfo.Text.Text = _serverPlayers.ToString() + " / " + _serverMaxPlayers.ToString();
            _lblPortInfo.Text.Text = _serverPort.ToString();
        }

        public void UpdateGUIPosition()
        {
            _imgMainBg.Position = new Vector2i(
                (int)((CluwneLib.Window.Viewport.Size.X / 2f) - (_imgMainBg.ClientArea.Width / 2f)),
                (int)((CluwneLib.Window.Viewport.Size.Y / 2f) - (_imgMainBg.ClientArea.Height / 2f)));
            _imgMainBg.Update(0);

            _recStatus = Box2.FromDimensions(_imgMainBg.Position.X + 10, _imgMainBg.Position.Y + 63, 785, 21);

            _imgStatus.Position = new Vector2i((int)_recStatus.Left,
                                               (int)_recStatus.Top);
            _imgStatus.Update(0);

            _lblServer.Position = new Vector2i((int)_recStatus.Left + 5,
                                               (int)_recStatus.Top + 2);
            _lblServer.Update(0);

            _lblServerInfo.Position = new Vector2i(_lblServer.ClientArea.Right,
                                                   _lblServer.ClientArea.Top);
            _lblServerInfo.Update(0);

            _lblMode.Position = new Vector2i(_lblServerInfo.ClientArea.Right + (int)_lastLblSpacing,
                                             _lblServerInfo.ClientArea.Top);
            _lblMode.Update(0);

            _lblModeInfo.Position = new Vector2i(_lblMode.ClientArea.Right,
                                                 _lblMode.ClientArea.Top);
            _lblModeInfo.Update(0);

            _lblPlayers.Position = new Vector2i(_lblModeInfo.ClientArea.Right + (int)_lastLblSpacing,
                                                _lblModeInfo.ClientArea.Top);
            _lblPlayers.Update(0);

            _lblPlayersInfo.Position = new Vector2i(_lblPlayers.ClientArea.Right,
                                                    _lblPlayers.ClientArea.Top);
            _lblPlayersInfo.Update(0);

            _lblPort.Position = new Vector2i(_lblPlayersInfo.ClientArea.Right + (int)_lastLblSpacing,
                                             _lblPlayersInfo.ClientArea.Top);
            _lblPort.Update(0);

            _lblPortInfo.Position = new Vector2i(_lblPort.ClientArea.Right,
                                                 _lblPort.ClientArea.Top);
            _lblPortInfo.Update(0);

            _tabs.Position = _imgMainBg.Position + new Vector2i(5, 90);
            _tabs.Update(0);

            _lobbyChat.Position = new Vector2i(_imgMainBg.ClientArea.Left + 12,
                                               _imgMainBg.ClientArea.Bottom - _lobbyChat.ClientArea.Height - 12); //Wish the chat box wasnt such shit. Then i wouldnt have to do this here.
            _lobbyChat.Update(0);

            _imgChatBg.Position = new Vector2i(_lobbyChat.ClientArea.Left - 6,
                                               _lobbyChat.ClientArea.Top - 9);
            _imgChatBg.Update(0);

            _btnReady.Position = new Vector2i(_lobbyChat.ClientArea.Right - _btnReady.ClientArea.Width - 5,
                                              _lobbyChat.ClientArea.Top - _btnReady.ClientArea.Height - 8);
            _btnReady.Update(0);

            _btnBack.Position = new Vector2i(_lobbyChat.ClientArea.Left - _btnBack.ClientArea.Width - 20,
                                             _lobbyChat.ClientArea.Bottom - _btnBack.ClientArea.Height);
            _btnBack.Update(0);
        }

        private void _btnReady_Clicked(ImageButton sender)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            playerManager.SendVerb("joingame", 0);
        }

        private void _btnBack_Clicked(ImageButton sender)
        {
            StateManager.RequestStateChange<MainScreen>();

            NetworkManager.ClientDisconnect("Client left the lobby.");
        }

        #endregion Startup, Shutdown, Update

        #region IState Members

        public void Render(FrameEventArgs e)
        {
            _background.Draw();
            UserInterfaceManager.Render(e);
        }

        public void FormResize()
        {
            //TODO: Figure out what to do with this
        }

        #endregion IState Members

        #region Input

        public void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp(KeyEventArgs e)
        {
            //TODO: Figure out what to do with this
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
            //TODO: Figure out what to do with this
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
            UserInterfaceManager.TextEntered(e); //KeyDown returns true if the click is handled by the ui component.
        }

        #endregion Input
    }
}
