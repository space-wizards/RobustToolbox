using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces.State;
using ClientServices.UserInterface.Components;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.State.States
{
    public class NewLobby : State, IState
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
        private readonly SimpleImage _mainbg;

        private readonly List<Label> _serverLabels = new List<Label>();

        private readonly TabContainer _tabCharacter;
        private readonly JobTab _tabJob;
        private readonly TabContainer _tabObserve;
        private readonly TabContainer _tabServer;
        private readonly TabbedMenu _tabs;

        private float _lastLblSpacing = 10;
        //TODO Actually calculate this and adjust all labels accordingly. Make sure we compensate if labels longer than status line.

        private RectangleF _recStatus;
        private TabContainer _tabActive;

        #endregion

        #region Properties

        #endregion

        public NewLobby(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _mainbg = new SimpleImage
                          {
                              Sprite = "lobby_mainbg"
                          };

            _imgStatus = new SimpleImage
                             {
                                 Sprite = "lobby_statusbar"
                             };

            _lblServer = new Label("SERVER:", "MICROGME", ResourceManager);
            _lblServer.Text.Color = Color.WhiteSmoke;
            _serverLabels.Add(_lblServer);

            _lblServerInfo = new Label("LLJK#1", "MICROGME", ResourceManager);
            _lblServerInfo.Text.Color = Color.DarkRed;
            _serverLabels.Add(_lblServerInfo);

            _lblMode = new Label("GAMEMODE:", "MICROGME", ResourceManager);
            _lblMode.Text.Color = Color.WhiteSmoke;
            _serverLabels.Add(_lblMode);

            _lblModeInfo = new Label("SECRET", "MICROGME", ResourceManager);
            _lblModeInfo.Text.Color = Color.DarkRed;
            _serverLabels.Add(_lblModeInfo);

            _lblPlayers = new Label("PLAYERS:", "MICROGME", ResourceManager);
            _lblPlayers.Text.Color = Color.WhiteSmoke;
            _serverLabels.Add(_lblPlayers);

            _lblPlayersInfo = new Label("17/32", "MICROGME", ResourceManager);
            _lblPlayersInfo.Text.Color = Color.DarkRed;
            _serverLabels.Add(_lblPlayersInfo);

            _lblPort = new Label("PORT:", "MICROGME", ResourceManager);
            _lblPort.Text.Color = Color.WhiteSmoke;
            _serverLabels.Add(_lblPort);

            _lblPortInfo = new Label("1212", "MICROGME", ResourceManager);
            _lblPortInfo.Text.Color = Color.DarkRed;
            _serverLabels.Add(_lblPortInfo);

            _tabs = new TabbedMenu
                        {
                            TopSprite = "lobby_tab_top",
                            MidSprite = "lobby_tab_mid",
                            BotSprite = "lobby_tab_bot",
                            TabOffset = new Point(-8, 300)
                        };

            _tabJob = new JobTab("lobbyTabJob", new Size(793, 450), ResourceManager)
                          {
                              tabSpriteName = "lobby_tab_bcase"
                          };
            _tabs.AddTab(_tabJob);

            _tabCharacter = new TabContainer("lobbyTabCharacter", new Size(793, 450), ResourceManager)
                                {
                                    tabSpriteName = "lobby_tab_person"
                                };
            _tabs.AddTab(_tabCharacter);

            _tabObserve = new TabContainer("lobbyTabObserve", new Size(793, 450), ResourceManager)
                              {
                                  tabSpriteName = "lobby_tab_eye"
                              };
            _tabs.AddTab(_tabObserve);

            _tabServer = new TabContainer("lobbyTabServer", new Size(793, 450), ResourceManager)
                             {
                                 tabSpriteName = "lobby_tab_info"
                             };
            _tabs.AddTab(_tabServer);

            _tabs.SelectTab(_tabJob);
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            UserInterfaceManager.AddComponent(_mainbg);
            UserInterfaceManager.AddComponent(_imgStatus);

            foreach (Label curr in _serverLabels)
                UserInterfaceManager.AddComponent(curr);

            UserInterfaceManager.AddComponent(_tabs);
        }

        public void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_mainbg);
            UserInterfaceManager.RemoveComponent(_imgStatus);
            UserInterfaceManager.RemoveComponent(_tabs);

            foreach (Label curr in _serverLabels)
                UserInterfaceManager.RemoveComponent(curr);
        }

        public void Update(FrameEventArgs e)
        {
            _mainbg.Position = new Point(
                (int) ((Gorgon.Screen.Width/2f) - (_mainbg.ClientArea.Width/2f)),
                (int) ((Gorgon.Screen.Height/2f) - (_mainbg.ClientArea.Height/2f)));

            _recStatus = new RectangleF(_mainbg.Position.X + 10, _mainbg.Position.Y + 63, 785, 21);

            _imgStatus.Position = new Point((int) _recStatus.Left, (int) _recStatus.Top);

            _lblServer.Position = new Point((int) _recStatus.Left + 5, (int) _recStatus.Top + 2);
            _lblServerInfo.Position = new Point(_lblServer.ClientArea.Right, _lblServer.ClientArea.Y);

            _lblMode.Position = new Point(_lblServerInfo.ClientArea.Right + (int) _lastLblSpacing,
                                          _lblServerInfo.ClientArea.Y);
            _lblModeInfo.Position = new Point(_lblMode.ClientArea.Right, _lblMode.ClientArea.Y);

            _lblPlayers.Position = new Point(_lblModeInfo.ClientArea.Right + (int) _lastLblSpacing,
                                             _lblModeInfo.ClientArea.Y);
            _lblPlayersInfo.Position = new Point(_lblPlayers.ClientArea.Right, _lblPlayers.ClientArea.Y);

            _lblPort.Position = new Point(_lblPlayersInfo.ClientArea.Right + (int) _lastLblSpacing,
                                          _lblPlayersInfo.ClientArea.Y);
            _lblPortInfo.Position = new Point(_lblPort.ClientArea.Right, _lblPort.ClientArea.Y);

            _tabs.Position = _mainbg.Position + new Size(5, 90);
        }

        #endregion

        #region IState Members

        public void GorgonRender(FrameEventArgs e)
        {
            _background.Draw(new Rectangle(0, 0, Gorgon.CurrentClippingViewport.Width,
                                           Gorgon.CurrentClippingViewport.Height));
            UserInterfaceManager.Render();
        }

        public void FormResize()
        {
        }

        #endregion

        #region Input

        public void KeyDown(KeyboardInputEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp(KeyboardInputEventArgs e)
        {
        }

        public void MouseUp(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        #endregion
    }
}