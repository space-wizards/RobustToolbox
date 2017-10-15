using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Lidgren.Network;
using OpenTK;
using OpenTK.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;

namespace SS14.Client.State.States
{
    /// <summary>
    ///     Main menu screen that is the first screen to be displayed when the game starts.
    /// </summary>
    // Instantiated dynamically through the StateManager.
    public class MainScreen : State
    {
        /// <summary>
        ///     Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public const ushort DefaultPort = 1212;

        private const float ConnectTimeOut = 5000.0f;

        private readonly Screen _uiScreen;

        private DateTime _connectTime;
        private bool _isConnecting;

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="managers">A dictionary of common managers from the IOC system, so you don't have to resolve them yourself.</param>
        public MainScreen(IDictionary<Type, object> managers) : base(managers)
        {
            _uiScreen = new Screen
            {
                Background = ResourceCache.GetSprite("ss14_logo_background"),
                Width  = (int)CluwneLib.Window.Viewport.Size.X,
                Height = (int)CluwneLib.Window.Viewport.Size.Y,
            };
            // UI screen is added in startup

            var imgTitle = new SimpleImage
            {
                Sprite = "ss14_logo",
                Alignment = Align.Right,
                LocalPosition = new Vector2i(-550, 100)
            };
            _uiScreen.AddComponent(imgTitle);

            var txtConnect = new Textbox(100, ResourceCache)
            {
                Text = ConfigurationManager.GetCVar<string>("net.server"),
                Alignment = Align.Left | Align.Bottom,
                LocalPosition = new Vector2i(10, 50)
            };
            txtConnect.OnSubmit += (text, sender) => { StartConnect(text); };
            imgTitle.AddComponent(txtConnect);

            var btnConnect = new ImageButton
            {
                ImageNormal = "connect_norm",
                ImageHover = "connect_hover",
                Alignment = Align.Left | Align.Bottom,
                LocalPosition = new Vector2i(0, 20)
            };
            btnConnect.Clicked += sender =>
            {
                if (!_isConnecting)
                {
                    StartConnect(txtConnect.Text);
                }
                else
                {
                    _isConnecting = false;
                    NetworkManager.ClientDisconnect("Client disconnected from game.");
                }
            };
            txtConnect.AddComponent(btnConnect);
            
            var btnOptions = new ImageButton
            {
                ImageNormal = "options_norm",
                ImageHover = "options_hover",
                Alignment = Align.Left | Align.Bottom,
                LocalPosition = new Vector2i(0, 20)
            };
            btnOptions.Clicked += sender =>
            {
                if (_isConnecting)
                {
                    _isConnecting = false;
                    NetworkManager.ClientDisconnect("Client disconnected from game.");
                }

                StateManager.RequestStateChange<OptionsMenu>();
            };
            btnConnect.AddComponent(btnOptions);
            
            var btnExit = new ImageButton
            {
                ImageNormal = "exit_norm",
                ImageHover = "exit_hover",
                Alignment = Align.Left | Align.Bottom,
                LocalPosition = new Vector2i(0, 20)
            };
            btnExit.Clicked += sender => CluwneLib.Stop();
            btnOptions.AddComponent(btnExit);

            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI", ResourceCache)
            {
                Text = { Color = new Color4(245, 245, 245, 255) },
                Alignment = Align.Left | Align.Bottom,
                LocalPosition = new Vector2i(3, -20), // no way to make this dynamic, need a Resize event :(
            };
            _uiScreen.AddComponent(lblVersion);
        }

        /// <inheritdoc />
        public override void FormResize()
        {
            _uiScreen.Width = (int)CluwneLib.Window.Viewport.Size.X;
            _uiScreen.Height = (int)CluwneLib.Window.Viewport.Size.Y;

            UserInterfaceManager.ResizeComponents();
            
            base.FormResize();
        }

        /// <inheritdoc />
        public override void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        /// <inheritdoc />
        public override void KeyUp(KeyEventArgs e) { }

        /// <inheritdoc />
        public override void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        /// <inheritdoc />
        public override void MouseDown(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MouseMoved(MouseMoveEventArgs e) { }

        /// <inheritdoc />
        public override void MousePressed(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MouseMove(MouseMoveEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        /// <inheritdoc />
        public override void MouseWheelMove(MouseWheelEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        /// <inheritdoc />
        public override void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }

        /// <inheritdoc />
        public override void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        /// <inheritdoc />
        public override void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            NetworkManager.ClientDisconnect("Client disconnected from game.");
            NetworkManager.Connected += OnConnected;

            UserInterfaceManager.AddComponent(_uiScreen);
            FormResize();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            NetworkManager.Connected -= OnConnected;

            UserInterfaceManager.RemoveComponent(_uiScreen);

            // There is no way to actually destroy a screen.
            //_uiScreen.Destroy();
            //_uiScreen = null;
        }

        /// <inheritdoc />
        public override void Update(FrameEventArgs e)
        {
            if (_isConnecting)
            {
                var dif = DateTime.Now - _connectTime;
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

        private void StartConnect(string address)
        {
            if (_isConnecting)
                return;

            // See if the IP includes a port.
            var split = address.Split(':');
            var ip = address;
            var port = DefaultPort;
            if (split.Length > 2)
                throw new InvalidOperationException("Not a valid Address.");

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                    throw new InvalidOperationException("Not a valid port.");
            }

            if (NetUtility.Resolve(ip, port) == null)
                throw new InvalidOperationException("Not a valid Address.");

            _connectTime = DateTime.Now;
            _isConnecting = true;
            NetworkManager.ClientConnect(ip, port);
        }
    }
}
