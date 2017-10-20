using System;
using System.Collections.Generic;
using Lidgren.Network;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Player;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using FrameEventArgs = SS14.Client.Graphics.FrameEventArgs;

namespace SS14.Client.State.States
{
    public class Lobby : State
    {
        private readonly Screen _uiScreen;

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

        //private readonly List<Label> _serverLabels = new List<Label>();

        private readonly TabContainer _tabCharacter;
        private readonly TabContainer _tabObserve;
        private readonly PlayerListTab _tabServer;
        private readonly TabbedMenu _tabs;
        private readonly SimpleImage _imgChatBg;
        private readonly ImageButton _btnReady;
        private readonly ImageButton _btnBack;

        private readonly float _lastLblSpacing = 10;

        private readonly Chatbox _lobbyChat;
        private int _prevScreenWidth;
        private int _prevScreenHeight;
        private Box2 _recStatus;

        private string _serverName;
        private int _serverPort;
        private string _welcomeString;
        private int _serverMaxPlayers;
        private int _serverPlayers;
        private string _serverMapName;
        private string _gameType;

        public Lobby(IDictionary<Type, object> managers)
            : base(managers)
        {
            _uiScreen = new Screen();
            _uiScreen.BackgroundImage = ResourceCache.GetSprite("ss14_logo_background");
            // UI screen is added in startup

            _imgMainBg = new SimpleImage();
            _imgMainBg.Sprite = "lobby_mainbg";
            _imgMainBg.Alignment = Align.HCenter | Align.VCenter;
            _uiScreen.AddControl(_imgMainBg);

            _imgStatus = new SimpleImage();
            _imgStatus.Sprite = "lobby_statusbar";
            _imgStatus.LocalPosition = new Vector2i(10, 63);
            _imgMainBg.AddControl(_imgStatus);

            _lblServer = new Label("SERVER: ", "MICROGME");
            _lblServer.ForegroundColor = new Color(245, 245, 245, 255);
            _lblServer.LocalPosition = new Vector2i(5, 2);
            _imgStatus.AddControl(_lblServer);

            _lblServerInfo = new Label("LLJK#1", "MICROGME");
            _lblServerInfo.ForegroundColor = new Color(139, 0, 0, 255);
            _lblServerInfo.FixedWidth = 100;
            _lblServerInfo.Alignment = Align.Right;
            _lblServer.AddControl(_lblServerInfo);

            _lblMode = new Label("GAMEMODE: ", "MICROGME");
            _lblMode.ForegroundColor = new Color(245, 245, 245, 255);
            _lblMode.Alignment = Align.Right;
            _lblMode.LocalPosition = new Vector2i(10, 0);
            _lblServerInfo.AddControl(_lblMode);

            _lblModeInfo = new Label("SECRET", "MICROGME");
            _lblModeInfo.ForegroundColor = new Color(139, 0, 0, 255);
            _lblModeInfo.FixedWidth = 90;
            _lblModeInfo.Alignment = Align.Right;
            _lblMode.AddControl(_lblModeInfo);

            _lblPlayers = new Label("PLAYERS: ", "MICROGME");
            _lblPlayers.ForegroundColor = new Color(245, 245, 245, 255);
            _lblPlayers.Alignment = Align.Right;
            _lblPlayers.LocalPosition = new Vector2i(10, 0);
            _lblModeInfo.AddControl(_lblPlayers);

            _lblPlayersInfo = new Label("17/32", "MICROGME");
            _lblPlayersInfo.ForegroundColor = new Color(139, 0, 0, 255);
            _lblPlayersInfo.FixedWidth = 60;
            _lblPlayersInfo.Alignment = Align.Right;
            _lblPlayers.AddControl(_lblPlayersInfo);

            _lblPort = new Label("PORT: ", "MICROGME");
            _lblPort.ForegroundColor = new Color(245, 245, 245, 255);
            _lblPort.Alignment = Align.Right;
            _lblPort.LocalPosition = new Vector2i(10, 0);
            _lblPlayersInfo.AddControl(_lblPort);

            _lblPortInfo = new Label(MainScreen.DefaultPort.ToString(), "MICROGME");
            _lblPortInfo.ForegroundColor = new Color(139, 0, 0, 255);
            _lblPortInfo.FixedWidth = 50;
            _lblPortInfo.Alignment = Align.Right;
            _lblPort.AddControl(_lblPortInfo);
            
            _tabs = new TabbedMenu();
            _tabs.TopSprite = "lobby_tab_top";
            _tabs.MidSprite = "lobby_tab_mid";
            _tabs.BotSprite = "lobby_tab_bot";
            _tabs.TabOffset = new Vector2i(-8, 300);
            _tabs.LocalPosition = new Vector2i(5, 90);
            _imgMainBg.AddControl(_tabs);

            _tabCharacter = new TabContainer("lobbyTabCharacter", new Vector2i(793, 450), ResourceCache);
            _tabCharacter.tabSpriteName = "lobby_tab_person";
            _tabs.AddTab(_tabCharacter);

            _tabObserve = new TabContainer("lobbyTabObserve", new Vector2i(793, 450), ResourceCache);
            _tabObserve.tabSpriteName = "lobby_tab_eye";
            _tabs.AddTab(_tabObserve);

            _tabServer = new PlayerListTab("lobbyTabServer", new Vector2i(793, 450), ResourceCache);
            _tabServer.tabSpriteName = "lobby_tab_info";
            _tabs.AddTab(_tabServer);
            _tabs.SelectTab(_tabServer);

            _imgChatBg = new SimpleImage();
            _imgChatBg.Sprite = "lobby_chatbg";
            _imgChatBg.Alignment = Align.HCenter | Align.Bottom;
            _imgChatBg.Resize += (sender, args) => { _imgChatBg.LocalPosition = new Vector2i(0, -9 + -_imgChatBg.Height); };
            _imgMainBg.AddControl(_imgChatBg);

            _lobbyChat = new Chatbox("lobbychat", new Vector2i(780, 225), ResourceCache);
            _lobbyChat.Alignment = Align.HCenter | Align.VCenter;
            _imgChatBg.AddControl(_lobbyChat);

            _btnReady = new ImageButton();
            _btnReady.ImageNormal = "lobby_ready";
            _btnReady.ImageHover = "lobby_ready_green";
            _btnReady.Alignment = Align.Right;
            _btnReady.Resize += (sender, args) => { _btnReady.LocalPosition = new Vector2i(-5 + -_btnReady.Width, -5 + -_btnReady.Height); };
            _imgChatBg.AddControl(_btnReady);
            _btnReady.Clicked += _btnReady_Clicked;
            
            _btnBack = new ImageButton();
            _btnBack.ImageNormal = "lobby_back";
            _btnBack.ImageHover = "lobby_back_green";
            _btnBack.Resize += (sender, args) => { _btnBack.LocalPosition = new Vector2i(-5 + -_btnBack.Width, 0); };
            _btnReady.AddControl(_btnBack);
            _btnBack.Clicked += _btnBack_Clicked;
        }

        public override void Startup()
        {
            UserInterfaceManager.AddComponent(_uiScreen);

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            var message = NetworkManager.CreateMessage();
            message.Write((byte) NetMessages.WelcomeMessageReq); //Request Welcome msg.
            NetworkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableOrdered);

            var playerListMsg = NetworkManager.CreateMessage();
            playerListMsg.Write((byte) NetMessages.PlayerListReq); //Request Playerlist.
            NetworkManager.ClientSendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_uiScreen);

            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
        }

        public override void Update(FrameEventArgs e)
        {
            // This might be a hacky solution, but the button loses focus way too fast.
            //_btnReady.Focus = true;

            _lblServerInfo.Text = _serverName;
            _lblModeInfo.Text = _gameType;
            _lblPlayersInfo.Text = _serverPlayers + " / " + _serverMaxPlayers;
            _lblPortInfo.Text = _serverPort.ToString();
        }

        public override void FormResize()
        {
            _uiScreen.Width = (int)CluwneLib.Window.Viewport.Size.X;
            _uiScreen.Height = (int)CluwneLib.Window.Viewport.Size.Y;

            UserInterfaceManager.ResizeComponents();
        }

        public override void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }
        
        public override void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public override void MouseDown(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }
        
        public override void MousePressed(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        public override void MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        public override void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }

        public override void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        public override void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e); //KeyDown returns true if the click is handled by the ui component.
        }

        private void NetworkManagerMessageArrived(object sender, NetMessageArgs args)
        {
            var message = args.RawMessage;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus) message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        var disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager,
                            UserInterfaceManager,
                            ResourceCache,
                            disconnectMessage));
                    }
                    break;

                case NetIncomingMessageType.Data:
                    var messageType = (NetMessages) message.ReadByte();
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
            var channel = (ChatChannel) msg.ReadByte();
            var text = msg.ReadString();
            var message = "[" + channel + "] " + text;
            _lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        private void HandlePlayerList(NetIncomingMessage message)
        {
            var playerCount = message.ReadByte();
            _serverPlayers = playerCount;
            _tabServer._scPlayerList.Components.Clear();
            var offY = 0;
            for (var i = 0; i < playerCount; i++)
            {
                var currName = message.ReadString();
                var currStatus = (SessionStatus) message.ReadByte();
                var currRoundtrip = message.ReadFloat();

                var newLabel = new Label(currName + "\t\tStatus: " + currStatus + "\t\tLatency: " + Math.Truncate(currRoundtrip * 1000) + " ms", "MICROGBE");
                newLabel.Position = new Vector2i(0, offY);
                newLabel.ForegroundColor = Color.Black;
                newLabel.Update(0);
                offY += newLabel.ClientArea.Height;
                _tabServer._scPlayerList.Components.Add(newLabel);
            }
        }

        private void HandleJoinGame()
        {
            StateManager.RequestStateChange<GameScreen>();
        }

        private void _btnReady_Clicked(ImageButton sender)
        {
            IoCManager.Resolve<IPlayerManager>().SendVerb("joingame", 0);
        }

        private void _btnBack_Clicked(ImageButton sender)
        {
            NetworkManager.ClientDisconnect("Client left the lobby.");
            StateManager.RequestStateChange<MainScreen>();
        }
    }
}
