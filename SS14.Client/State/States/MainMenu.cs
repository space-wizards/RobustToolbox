using Lidgren.Network;
using OpenTK;
using OpenTK.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using SS14.Client.UserInterface;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.State.States
{
    public class MainScreen : State, IState
    {
        /// <summary>
        /// Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public const ushort DEFAULT_PORT = 1212;
        private const float ConnectTimeOut = 5000.0f;

        private Screen _uiScreen;

        private readonly ImageButton _btnConnect;
        private readonly ImageButton _btnExit;
        private readonly ImageButton _btnOptions;
        private readonly Textbox _txtConnect;
        private readonly Label _lblVersion;
        private readonly SimpleImage _imgTitle;

        private DateTime _connectTime;
        private bool _isConnecting;

        public MainScreen(IDictionary<Type, object> managers) : base(managers)
        {
            _uiScreen = new Screen
            {
                Background = ResourceCache.GetSprite("ss14_logo_background")
            };

            _btnConnect = new ImageButton
            {
                ImageNormal = "connect_norm",
                ImageHover = "connect_hover"
            };
            _btnConnect.Clicked += sender =>
            {
                if (!_isConnecting)
                    StartConnect(_txtConnect.Text);
                else
                {
                    _isConnecting = false;
                    NetworkManager.ClientDisconnect("Client disconnected from game.");
                }
            };

            _btnOptions = new ImageButton
            {
                ImageNormal = "options_norm",
                ImageHover = "options_hover"
            };
            _btnOptions.Clicked += sender =>
            {
                if (_isConnecting)
                {
                    _isConnecting = false;
                    NetworkManager.ClientDisconnect("Client disconnected from game.");
                }

                StateManager.RequestStateChange<OptionsMenu>();
            };

            _btnExit = new ImageButton
            {
                ImageNormal = "exit_norm",
                ImageHover = "exit_hover"
            };
            _btnExit.Clicked += sender => CluwneLib.Stop();

            _txtConnect = new Textbox(100, ResourceCache)
            {
                Text = ConfigurationManager.GetCVar<string>("net.server")
            };
            _txtConnect.OnSubmit += (text, sender) =>
            {
                StartConnect(text);
            };

            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            _lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceCache)
            {
                Text =
                {
                    Color = new Color4(245, 245, 245, 255)
                },
            };

            _imgTitle = new SimpleImage
            {
                Sprite = "ss14_logo",
            };

            _uiScreen.AddComponent(_imgTitle);
            _uiScreen.AddComponent(_txtConnect);
            _uiScreen.AddComponent(_btnConnect);
            _uiScreen.AddComponent(_btnOptions);
            _uiScreen.AddComponent(_btnExit);
            _uiScreen.AddComponent(_lblVersion);

            FormResize();
        }

        public void Render(FrameEventArgs e) { }

        public void FormResize()
        {
            var _width = (int)CluwneLib.Window.Viewport.Size.X;
            var _height = (int)CluwneLib.Window.Viewport.Size.Y;

            _uiScreen.Width = _width;
            _uiScreen.Height = _height;

            _lblVersion.Position = new Vector2i(_width - _lblVersion.ClientArea.Width - 3,
                                                _height - _lblVersion.ClientArea.Height - 3);
            _lblVersion.Update(0);
            _imgTitle.Position = new Vector2i(_width - 550, 100);
            _imgTitle.Update(0);
            _txtConnect.Position = new Vector2i(_imgTitle.ClientArea.Left + 10, _imgTitle.ClientArea.Bottom + 50);
            _txtConnect.Update(0);
            _btnConnect.Position = new Vector2i(_txtConnect.Position.X, _txtConnect.ClientArea.Bottom + 20);
            _btnConnect.Update(0);
            _btnOptions.Position = new Vector2i(_btnConnect.Position.X, _btnConnect.ClientArea.Bottom + 20);
            _btnOptions.Update(0);
            _btnExit.Position = new Vector2i(_btnOptions.Position.X, _btnOptions.ClientArea.Bottom + 20);
            _btnExit.Update(0);
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

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.ClientDisconnect("Client disconnected from game.");
            NetworkManager.Connected += OnConnected;

            UserInterfaceManager.AddComponent(_uiScreen);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_uiScreen);

            // 
            //_uiScreen.Destroy();
            //_uiScreen = null;
        }

        public void Update(FrameEventArgs e)
        {
            if (_isConnecting)
            {
                TimeSpan dif = DateTime.Now - _connectTime;
                if (dif.TotalMilliseconds > ConnectTimeOut)
                {
                    _isConnecting = false;
                    NetworkManager.ClientDisconnect("Client timed out.");
                }
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _isConnecting = false;
            StateManager.RequestStateChange<Lobby>();
        }

        public void StartConnect(string address)
        {
            if (_isConnecting)
            {
                return;
            }

            // See if the IP includes a port.
            var split = address.Split(':');
            string ip = address;
            ushort port = DEFAULT_PORT;
            if (split.Length > 2)
            {
                // Multiple colons?
                throw new InvalidOperationException("Not a valid Address.");
            }

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                {
                    throw new InvalidOperationException("Not a valid port.");
                }
            }

            if (NetUtility.Resolve(ip, port) == null)
            {
                throw new InvalidOperationException("Not a valid Address.");
            }

            _connectTime = DateTime.Now;
            _isConnecting = true;
            NetworkManager.ClientConnect(ip, port);
        }

        #endregion Startup, Shutdown, Update
    }
}
