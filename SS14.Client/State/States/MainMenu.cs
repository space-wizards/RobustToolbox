using System;
using System.Text.RegularExpressions;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Client.Interfaces.State;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;

namespace SS14.Client.State.States
{
    /// <summary>
    ///     Main menu screen that is the first screen to be displayed when the game starts.
    /// </summary>
    // Instantiated dynamically through the StateManager.
    public class MainScreen : State
    {
        private const string PublicServerAddress = "server.spacestation14.io";

        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IUserInterfaceManager userInterfaceManager;
        [Dependency] private readonly IStateManager stateManager;
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;

        private MainMenuControl _mainMenuControl;
        private OptionsMenu OptionsMenu;
        private bool _isConnecting;

        // ReSharper disable once InconsistentNaming
        private static readonly Regex IPv6Regex = new Regex(@"\[(.*:.*:.*)](?::(\d+))?");

        /// <inheritdoc />
        public override void Startup()
        {
            IoCManager.InjectDependencies(this);

            _mainMenuControl = new MainMenuControl();
            userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

            _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
            _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
            _mainMenuControl.DirectConnectButton.OnPressed += DirectConnectButtonPressed;
            _mainMenuControl.JoinPublicServerButton.OnPressed += JoinPublicServerButtonPressed;
            _mainMenuControl.AddressBox.OnTextEntered += AddressBoxEntered;

            _client.RunLevelChanged += RunLevelChanged;

            OptionsMenu = new OptionsMenu
            {
                Visible = false,
            };
            OptionsMenu.AddToScreen();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _client.RunLevelChanged -= RunLevelChanged;
            _netManager.ConnectFailed -= _onConnectFailed;

            _mainMenuControl.Dispose();
            OptionsMenu.Dispose();
        }

        private void QuitButtonPressed(BaseButton.ButtonEventArgs args)
        {
            IoCManager.Resolve<IGameControllerProxy>().GameController.Shutdown();
        }

        private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
        {
            OptionsMenu.OpenCentered();
        }

        private void DirectConnectButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var input = _mainMenuControl.AddressBox;
            TryConnect(input.Text);
        }

        private void JoinPublicServerButtonPressed(BaseButton.ButtonEventArgs args)
        {
            TryConnect(PublicServerAddress);
        }

        private void AddressBoxEntered(LineEdit.LineEditEventArgs args)
        {
            if (_isConnecting)
            {
                return;
            }

            TryConnect(args.Text);
        }

        private void TryConnect(string address)
        {
            var configName = _configurationManager.GetCVar<string>("player.name");
            if (_mainMenuControl.UserNameBox.Text != configName)
            {
                _configurationManager.SetCVar("player.name", _mainMenuControl.UserNameBox.Text);
                _configurationManager.SaveToFile();
            }
            _setConnectingState(true);
            _netManager.ConnectFailed += _onConnectFailed;
            try
            {
                ParseAddress(address, out var ip, out var port);
                _client.ConnectToServer(ip, port);
            }
            catch (ArgumentException e)
            {
                userInterfaceManager.Popup($"Unable to connect: {e.Message}", "Connection error.");
                Logger.Warning(e.ToString());
                _netManager.ConnectFailed -= _onConnectFailed;
            }
        }

        private void RunLevelChanged(object obj, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.InGame)
            {
                stateManager.RequestStateChange<GameScreen>();
            }
            else if (args.NewLevel == ClientRunLevel.Initialize)
            {
                _setConnectingState(false);
                _netManager.ConnectFailed -= _onConnectFailed;
            }
        }

        private void ParseAddress(string address, out string ip, out ushort port)
        {
            var match6 = IPv6Regex.Match(address);
            if (match6 != Match.Empty)
            {
                ip = match6.Groups[1].Value;
                if (!match6.Groups[2].Success)
                {
                    port = _client.DefaultPort;
                }
                else if (!ushort.TryParse(match6.Groups[2].Value, out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }

                return;
            }

            // See if the IP includes a port.
            var split = address.Split(':');
            ip = address;
            port = _client.DefaultPort;
            if (split.Length > 2)
            {
                throw new ArgumentException("Not a valid Address.");
            }

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }
            }
        }

        private void _onConnectFailed(object _, NetConnectFailArgs args)
        {
            userInterfaceManager.Popup($"Failed to connect:\n{args.Reason}");
            _netManager.ConnectFailed -= _onConnectFailed;
            _setConnectingState(false);
        }

        private void _setConnectingState(bool state)
        {
            _isConnecting = state;
            _mainMenuControl.DirectConnectButton.Disabled = state;
#if RELEASE
            _mainMenuControl.JoinPublicServerButton.Disabled = state;
#endif
        }

        private sealed class MainMenuControl : Control
        {
            [Dependency] private readonly IResourceCache _resourceCache;
            [Dependency] private readonly IConfigurationManager _configurationManager;

            public LineEdit UserNameBox { get; private set; }
            public Button JoinPublicServerButton { get; private set; }
            public LineEdit AddressBox { get; private set; }
            public Button DirectConnectButton { get; private set; }
            public Button OptionsButton { get; private set; }
            public Button QuitButton { get; private set; }

            protected override void Initialize()
            {
                base.Initialize();

                IoCManager.InjectDependencies(this);

                MouseFilter = MouseFilterMode.Ignore;

                SetAnchorAndMarginPreset(LayoutPreset.Wide);

                var vBox = new VBoxContainer
                {
                    AnchorLeft = 1,
                    AnchorRight = 1,
                    AnchorBottom = 0,
                    AnchorTop = 0,
                    MarginTop = 30,
                    MarginLeft = -350,
                    MarginRight = -25,
                    MarginBottom = 0,
                    StyleIdentifier = "mainMenuVBox",
                };
                AddChild(vBox);

                var logoTexture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png");
                var logo = new TextureRect
                {
                    Texture = logoTexture,
                    Stretch = TextureRect.StretchMode.KeepCentered,
                };
                vBox.AddChild(logo);

                var userNameHBox = new HBoxContainer { SeparationOverride = 4};
                vBox.AddChild(userNameHBox);
                userNameHBox.AddChild(new Label {Text = "Username:"});

                var currentUserName = _configurationManager.GetCVar<string>("player.name");
                UserNameBox = new LineEdit
                {
                    Text = currentUserName, PlaceHolder = "Username",
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };

                userNameHBox.AddChild(UserNameBox);

                JoinPublicServerButton = new Button
                {
                    Text = "Join Public Server",
                    TextAlign = Button.AlignMode.Center,
#if !RELEASE
                    Disabled = true,
                    ToolTip = "Cannot connect to public server with a debug build."
#endif
                };

                vBox.AddChild(JoinPublicServerButton);

                AddressBox = new LineEdit
                {
                    Text = "localhost",
                    PlaceHolder = "server address:port",
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };

                vBox.AddChild(AddressBox);

                DirectConnectButton = new Button
                {
                    Text = "Direct Connect",
                    TextAlign = Button.AlignMode.Center
                };

                vBox.AddChild(DirectConnectButton);

                OptionsButton = new Button
                {
                    Text = "Options",
                    TextAlign = Button.AlignMode.Center
                };

                vBox.AddChild(OptionsButton);

                QuitButton = new Button
                {
                    Text = "Quit",
                    TextAlign = Button.AlignMode.Center
                };

                vBox.AddChild(QuitButton);
            }
        }
    }
}
