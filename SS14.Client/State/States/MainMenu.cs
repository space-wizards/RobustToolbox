using Lidgren.Network;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.State.States
{
    public class MainScreen : State, IState
    {
        #region Fields

        /// <summary>
        /// Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public const ushort DEFAULT_PORT = 1212;
        private const float ConnectTimeOut = 5000.0f;
        private readonly List<FloatingDecoration> DecoFloats = new List<FloatingDecoration>();

        private readonly Sprite _background;
        private readonly ImageButton _btnConnect;
        private readonly ImageButton _btnExit;
        private readonly ImageButton _btnOptions;
        private readonly Textbox _txtConnect;
        private readonly Label _lblVersion;
        private readonly SimpleImage _imgTitle;

        private DateTime _connectTime;
        private bool _isConnecting;

        private int _Width;
        private int _Height;

        #endregion Fields

        public MainScreen(IDictionary<Type, object> managers) : base(managers)
        {
            _Width = (int)CluwneLib.Window.Viewport.Size.X;
            _Height = (int)CluwneLib.Window.Viewport.Size.Y;
            _background = ResourceCache.GetSprite("ss14_logo_background");

            _btnConnect = new ImageButton
            {
                ImageNormal = "connect_norm",
                ImageHover = "connect_hover"
            };
            _btnConnect.Clicked += _buttConnect_Clicked;

            _btnOptions = new ImageButton
            {
                ImageNormal = "options_norm",
                ImageHover = "options_hover"
            };
            _btnOptions.Clicked += _buttOptions_Clicked;

            _btnExit = new ImageButton
            {
                ImageNormal = "exit_norm",
                ImageHover = "exit_hover"
            };
            _btnExit.Clicked += _buttExit_Clicked;

            _txtConnect = new Textbox(100, ResourceCache) { Text = ConfigurationManager.GetCVar<string>("net.server") };
            _txtConnect.Position = new Vector2i(_Width / 3, _Height / 2);
            _txtConnect.OnSubmit += ConnectTextboxOnSubmit;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            _lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceCache);
            _lblVersion.Text.FillColor = new Color(245, 245, 245);

            _lblVersion.Position = new Vector2i(_Width - _lblVersion.ClientArea.Width - 3,
                                             _Height - _lblVersion.ClientArea.Height - 3);

            _imgTitle = new SimpleImage
            {
                Sprite = "ss14_logo",
            };

            FormResize();
        }

        #region IState Members

        public void Render(FrameEventArgs e)
        {
            _background.Draw();
        }

        public void FormResize()
        {
            _Width = (int)CluwneLib.Window.Viewport.Size.X;
            _Height = (int)CluwneLib.Window.Viewport.Size.Y;
            _background.Scale = new Vector2((float)_Width / _background.TextureRect.Width, (float)_Height / _background.TextureRect.Height);
            _lblVersion.Position = new Vector2i(_Width - _lblVersion.ClientArea.Width - 3,
                                                _Height - _lblVersion.ClientArea.Height - 3);
            _lblVersion.Update(0);
            _imgTitle.Position = new Vector2i(_Width - 550, 100);
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
        #endregion IState Members

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

        public void MouseWheelMove(MouseWheelScrollEventArgs e)
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

        private void _buttExit_Clicked(ImageButton sender)
        {
            CluwneLib.Stop();
        }

        private void _buttOptions_Clicked(ImageButton sender)
        {
            if (_isConnecting)
            {
                _isConnecting = false;
                NetworkManager.ClientDisconnect("Client disconnected from game.");
            }

            StateManager.RequestStateChange<OptionsMenu>();
        }

        private void _buttConnect_Clicked(ImageButton sender)
        {
            if (!_isConnecting)
                StartConnect(_txtConnect.Text);
            else
            {
                _isConnecting = false;
                NetworkManager.ClientDisconnect("Client disconnected from game.");
            }
        }

        private void ConnectTextboxOnSubmit(string text, Textbox sender)
        {
            StartConnect(text);
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.ClientDisconnect("Client disconnected from game.");
            NetworkManager.Connected += OnConnected;

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.AddComponent(floatingDeco);

            UserInterfaceManager.AddComponent(_txtConnect);
            UserInterfaceManager.AddComponent(_btnConnect);
            UserInterfaceManager.AddComponent(_btnOptions);
            UserInterfaceManager.AddComponent(_btnExit);
            UserInterfaceManager.AddComponent(_imgTitle);
            UserInterfaceManager.AddComponent(_lblVersion);
        }

        public void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_txtConnect);
            UserInterfaceManager.RemoveComponent(_btnConnect);
            UserInterfaceManager.RemoveComponent(_btnOptions);
            UserInterfaceManager.RemoveComponent(_btnExit);
            UserInterfaceManager.RemoveComponent(_imgTitle);
            UserInterfaceManager.RemoveComponent(_lblVersion);

            foreach (FloatingDecoration floatingDeco in DecoFloats)
                UserInterfaceManager.RemoveComponent(floatingDeco);

            DecoFloats.Clear();
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
