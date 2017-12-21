using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using SS14.Client.Input;
using SS14.Client.Interfaces;
//using SS14.Client.UserInterface;
//using SS14.Client.UserInterface.Controls;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Log;

namespace SS14.Client.State.States
{
    /// <summary>
    ///     Main menu screen that is the first screen to be displayed when the game starts.
    /// </summary>
    // Instantiated dynamically through the StateManager.
    public class MainScreen : State
    {
        private IBaseClient _client;
        //private Screen _uiScreen;

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="managers">A dictionary of common managers from the IOC system, so you don't have to resolve them yourself.</param>
        public MainScreen(IDictionary<Type, object> managers) : base(managers) { }

        /// <inheritdoc />
        public override void InitializeGUI()
        {
            _client = IoCManager.Resolve<IBaseClient>();
            /*
            _uiScreen = new Screen();
            _uiScreen.BackgroundImage = ResourceCache.GetSprite("ss14_logo_background");
            // UI screen is added in startup

            var imgTitle = new SimpleImage();
            imgTitle.Sprite = "ss14_logo";
            imgTitle.Alignment = Align.Right;
            imgTitle.LocalPosition = new Vector2i(-550, 100);
            _uiScreen.AddControl(imgTitle);

            var txtConnect = new Textbox(100);
            txtConnect.Text = ConfigurationManager.GetCVar<string>("net.server");
            txtConnect.Alignment = Align.Left | Align.Bottom;
            txtConnect.LocalPosition = new Vector2i(10, 50);
            txtConnect.OnSubmit += (sender, text) =>
            {
                if (_client.RunLevel == ClientRunLevel.Initialize)
                    if (TryParseAddress(text, out var ip, out var port))
                        _client.ConnectToServer(ip, port);
                //TODO: Else notify user that textbox address is not valid
            };
            imgTitle.AddControl(txtConnect);

            var btnConnect = new ImageButton();
            btnConnect.ImageNormal = "connect_norm";
            btnConnect.ImageHover = "connect_hover";
            btnConnect.Alignment = Align.Left | Align.Bottom;
            btnConnect.LocalPosition = new Vector2i(0, 20);
            btnConnect.Clicked += sender =>
            {
                if (_client.RunLevel == ClientRunLevel.Initialize)
                    if (TryParseAddress(txtConnect.Text, out var ip, out var port))
                        _client.ConnectToServer(ip, port);
            };
            txtConnect.AddControl(btnConnect);

            var btnOptions = new ImageButton();
            btnOptions.ImageNormal = "options_norm";
            btnOptions.ImageHover = "options_hover";
            btnOptions.Alignment = Align.Left | Align.Bottom;
            btnOptions.LocalPosition = new Vector2i(0, 20);
            btnOptions.Clicked += sender =>
            {
                if (_client.RunLevel <= ClientRunLevel.Initialize)
                    StateManager.RequestStateChange<OptionsMenu>();
            };
            btnConnect.AddControl(btnOptions);

            var btnExit = new ImageButton();
            btnExit.ImageNormal = "exit_norm";
            btnExit.ImageHover = "exit_hover";
            btnExit.Alignment = Align.Left | Align.Bottom;
            btnExit.LocalPosition = new Vector2i(0, 20);
            btnExit.Clicked += sender => CluwneLib.Stop();
            btnOptions.AddControl(btnExit);

            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var lblVersion = new Label("v. " + fvi.FileVersion, "CALIBRI");
            lblVersion.ForegroundColor = new Color(245, 245, 245);
            lblVersion.Alignment = Align.Right | Align.Bottom;
            lblVersion.Resize += (sender, args) => { lblVersion.LocalPosition = new Vector2i(-3 + -lblVersion.ClientArea.Width, -3 + -lblVersion.ClientArea.Height); };
            _uiScreen.AddControl(lblVersion);*/
        }

        /// <inheritdoc />
        public override void FormResize()
        {
            //_uiScreen.Width = (int)CluwneLib.Window.Viewport.Size.X;
            //_uiScreen.Height = (int)CluwneLib.Window.Viewport.Size.Y;

            //UserInterfaceManager.ResizeComponents();
        }

        /// <inheritdoc />
        public override void KeyDown(KeyEventArgs e)
        {
            //UserInterfaceManager.KeyDown(e);
        }

        /// <inheritdoc />
        public override void MouseUp(MouseButtonEventArgs e)
        {
            //UserInterfaceManager.MouseUp(e);
        }

        /// <inheritdoc />
        public override void MouseDown(MouseButtonEventArgs e)
        {
            //UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MousePressed(MouseButtonEventArgs e)
        {
            //UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MouseMove(MouseMoveEventArgs e)
        {
            //UserInterfaceManager.MouseMove(e);
        }

        /// <inheritdoc />
        public override void MouseWheelMove(MouseWheelEventArgs e)
        {
            //UserInterfaceManager.MouseWheelMove(e);
        }

        /// <inheritdoc />
        public override void MouseEntered(EventArgs e)
        {
            //UserInterfaceManager.MouseEntered(e);
        }

        /// <inheritdoc />
        public override void MouseLeft(EventArgs e)
        {
            //UserInterfaceManager.MouseLeft(e);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            Logger.Debug("We Main Menu now!");
            _client.RunLevelChanged += RunLevelChanged;

            //UserInterfaceManager.AddComponent(_uiScreen);
            FormResize();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _client.RunLevelChanged -= RunLevelChanged;

            //UserInterfaceManager.RemoveComponent(_uiScreen);

            // There is no way to actually destroy a screen.
            //_uiScreen.Destroy();
            //_uiScreen = null;
        }

        private void RunLevelChanged(object obj, RunLevelChangedEventArgs args)
        {
            //if (args.NewLevel == ClientRunLevel.Lobby)
            //    StateManager.RequestStateChange<Lobby>();
        }

        private bool TryParseAddress(string address, out string ip, out ushort port)
        {
            // See if the IP includes a port.
            var split = address.Split(':');
            ip = address;
            port = _client.DefaultPort;
            if (split.Length > 2)
                return false;
            //throw new InvalidOperationException("Not a valid Address.");

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                    return false;
                //throw new InvalidOperationException("Not a valid port.");
            }
            return true;
        }
    }
}
