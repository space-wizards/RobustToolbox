using Lidgren.Network;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Player;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using System;
using System.Collections.Generic;

namespace SS14.Client.State.States
{
    public class Lobby : State
    {
        private IPlayerManager _plyrMan;

        private Screen _uiScreen;

        private Label _lblModeInfo;
        private Label _lblPlayersInfo;
        private Label _lblPortInfo;
        private Label _lblServerInfo;

        private PlayerListTab _tabServer;
        private TabbedMenu _tabs;

        private Chatbox _lobbyChat;
        
        public Lobby(IDictionary<Type, object> managers)
            : base(managers) { }

        public override void InitializeGUI()
        {
            _uiScreen = new Screen();
            _uiScreen.BackgroundImage = ResourceCache.GetSprite("ss14_logo_background");
            // UI screen is added in startup

            var imgMainBg = new SimpleImage();
            imgMainBg.Sprite = "lobby_mainbg";
            imgMainBg.Alignment = Align.HCenter | Align.VCenter;
            _uiScreen.AddControl(imgMainBg);

            var imgStatus = new SimpleImage();
            imgStatus.Sprite = "lobby_statusbar";
            imgStatus.LocalPosition = new Vector2i(10, 63);
            imgMainBg.AddControl(imgStatus);

            var lblServer = new Label("SERVER: ", "MICROGME");
            lblServer.ForegroundColor = new Color(245, 245, 245);
            lblServer.LocalPosition = new Vector2i(5, 2);
            imgStatus.AddControl(lblServer);

            _lblServerInfo = new Label("------", "MICROGME");
            _lblServerInfo.ForegroundColor = new Color(139, 0, 0);
            _lblServerInfo.FixedWidth = 100;
            _lblServerInfo.Alignment = Align.Right;
            lblServer.AddControl(_lblServerInfo);

            var lblMode = new Label("GAMEMODE: ", "MICROGME");
            lblMode.ForegroundColor = new Color(245, 245, 245);
            lblMode.Alignment = Align.Right;
            lblMode.LocalPosition = new Vector2i(10, 0);
            _lblServerInfo.AddControl(lblMode);

            _lblModeInfo = new Label("------", "MICROGME");
            _lblModeInfo.ForegroundColor = new Color(139, 0, 0);
            _lblModeInfo.FixedWidth = 90;
            _lblModeInfo.Alignment = Align.Right;
            lblMode.AddControl(_lblModeInfo);

            var lblPlayers = new Label("PLAYERS: ", "MICROGME");
            lblPlayers.ForegroundColor = new Color(245, 245, 245);
            lblPlayers.Alignment = Align.Right;
            lblPlayers.LocalPosition = new Vector2i(10, 0);
            _lblModeInfo.AddControl(lblPlayers);

            _lblPlayersInfo = new Label("--/--", "MICROGME");
            _lblPlayersInfo.ForegroundColor = new Color(139, 0, 0);
            _lblPlayersInfo.FixedWidth = 60;
            _lblPlayersInfo.Alignment = Align.Right;
            lblPlayers.AddControl(_lblPlayersInfo);

            var lblPort = new Label("PORT: ", "MICROGME");
            lblPort.ForegroundColor = new Color(245, 245, 245);
            lblPort.Alignment = Align.Right;
            lblPort.LocalPosition = new Vector2i(10, 0);
            _lblPlayersInfo.AddControl(lblPort);

            _lblPortInfo = new Label("----", "MICROGME");
            _lblPortInfo.ForegroundColor = new Color(139, 0, 0);
            _lblPortInfo.FixedWidth = 50;
            _lblPortInfo.Alignment = Align.Right;
            lblPort.AddControl(_lblPortInfo);

            _tabs = new TabbedMenu();
            _tabs.TopSprite = "lobby_tab_top";
            _tabs.MidSprite = "lobby_tab_mid";
            _tabs.BotSprite = "lobby_tab_bot";
            _tabs.TabOffset = new Vector2i(-8, 300);
            _tabs.LocalPosition = new Vector2i(5, 90);
            imgMainBg.AddControl(_tabs);

            var tabCharacter = new TabContainer(new Vector2i(793, 350));
            tabCharacter.TabSpriteName = "lobby_tab_person";
            _tabs.AddTab(tabCharacter);

            var tabObserve = new TabContainer(new Vector2i(793, 350));
            tabObserve.TabSpriteName = "lobby_tab_eye";
            _tabs.AddTab(tabObserve);

            _tabServer = new PlayerListTab(new Vector2i(793, 350));
            _tabServer.TabSpriteName = "lobby_tab_info";
            _tabs.AddTab(_tabServer);
            _tabs.SelectTab(_tabServer);

            var imgChatBg = new SimpleImage();
            imgChatBg.Sprite = "lobby_chatbg";
            imgChatBg.Alignment = Align.HCenter | Align.Bottom;
            imgChatBg.Resize += (sender, args) => { imgChatBg.LocalPosition = new Vector2i(0, -9 + -imgChatBg.Height); };
            imgMainBg.AddControl(imgChatBg);

            _lobbyChat = new Chatbox(new Vector2i(780, 225));
            _lobbyChat.Alignment = Align.HCenter | Align.VCenter;
            imgChatBg.AddControl(_lobbyChat);

            var btnReady = new ImageButton();
            btnReady.ImageNormal = "lobby_ready";
            btnReady.ImageHover = "lobby_ready_green";
            btnReady.Alignment = Align.Right;
            btnReady.Resize += (sender, args) => { btnReady.LocalPosition = new Vector2i(-5 + -btnReady.Width, -5 + -btnReady.Height); };
            imgChatBg.AddControl(btnReady);
            btnReady.Clicked += _btnReady_Clicked;

            var btnBack = new ImageButton();
            btnBack.ImageNormal = "lobby_back";
            btnBack.ImageHover = "lobby_back_green";
            btnBack.Resize += (sender, args) => { btnBack.LocalPosition = new Vector2i(-5 + -btnBack.Width, 0); };
            btnReady.AddControl(btnBack);
            btnBack.Clicked += _btnBack_Clicked;
        }

        public override void Startup()
        {
            _plyrMan = IoCManager.Resolve<IPlayerManager>();
            _plyrMan.PlayerListUpdated += HandlePlayerList;

            UserInterfaceManager.AddComponent(_uiScreen);

            UpdateInfo();
            FormResize();
        }

        public override void Shutdown()
        {
            _plyrMan = IoCManager.Resolve<IPlayerManager>();
            _plyrMan.PlayerListUpdated -= HandlePlayerList;

            UserInterfaceManager.RemoveComponent(_uiScreen);
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
            UserInterfaceManager.TextEntered(e);
        }

        /// <summary>
        ///     Updates the visual player list in the tab.
        /// </summary>
        private void UpdatePlayerList(IReadOnlyDictionary<int, PlayerSession> sessions)
        {
            var playerCount = _plyrMan.MaxPlayers;
            
            _tabServer.PlayerList.DisposeAllChildren();
            var offY = 0;
            for (var i = 0; i < playerCount; i++)
            {
                var labelText = $"[{i+1}]";
                if (sessions.TryGetValue(i, out var session))
                {
                    labelText = labelText + $" {session.Name}\t\tStatus: {session.Status}\t\tLatency: {session.Ping} ms";
                }
                else
                {
                    labelText = labelText + $" <EMPTY>";
                }

                var newLabel = new Label(labelText, "MICROGBE");
                newLabel.Position = new Vector2i(0, offY);
                newLabel.ForegroundColor = Color.Black;
                newLabel.DoLayout();
                offY += newLabel.ClientArea.Height;
                _tabServer.PlayerList.AddControl(newLabel);
            }
        }

        /// <summary>
        ///     Updates the visual server info at the top of the lobby.
        /// </summary>
        private void UpdateServerInfo(ServerInfo info)
        {
            _lblServerInfo.Text = info.ServerName;
            _lblModeInfo.Text = info.GameMode;
            _lblPlayersInfo.Text = info.ServerPlayerCount + " / " + info.ServerMaxPlayers;
            _lblPortInfo.Text = info.ServerPort.ToString();
        }

#if _delme
        private void NetworkManagerMessageArrived(object sender, NetMessageArgs args)
        {
            var message = args.RawMessage;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus)message.ReadByte();
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
                    var messageType = (NetMessages)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessages.LobbyChat:
                            //TODO: Send player messages to a lobby chat
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
#endif
        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            var text = msg.ReadString();
            var message = "[" + channel + "] " + text;
            _lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        private void HandlePlayerList(object sender, EventArgs args)
        {
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            var man = IoCManager.Resolve<IPlayerManager>();
            UpdatePlayerList(man.SessionsDict);

            var clBase = IoCManager.Resolve<IBaseClient>();
            UpdateServerInfo(clBase.GameInfo);
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
