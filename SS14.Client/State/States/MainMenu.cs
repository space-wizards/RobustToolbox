using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Log;
using SS14.Client.Interfaces.State;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Utility;

namespace SS14.Client.State.States
{
    /// <summary>
    ///     Main menu screen that is the first screen to be displayed when the game starts.
    /// </summary>
    // Instantiated dynamically through the StateManager.
    public class MainScreen : State
    {
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IUserInterfaceManager userInterfaceManager;
        [Dependency] private readonly IStateManager stateManager;
        [Dependency] private readonly IClientNetManager _netManager;

        private MainMenuControl _mainMenuControl;

        private OptionsMenu OptionsMenu;
        private Button ConnectButton;

        // ReSharper disable once InconsistentNaming
        private static readonly Regex IPv6Regex = new Regex(@"\[(.*:.*:.*)](?::(\d+))?");

        /// <inheritdoc />
        public override void Startup()
        {
            IoCManager.InjectDependencies(this);

            _mainMenuControl = new MainMenuControl();
            userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

            var VBox = _mainMenuControl.GetChild("VBoxContainer");
            VBox.GetChild<Button>("ExitButton").OnPressed += ExitButtonPressed;
            VBox.GetChild<Button>("OptionsButton").OnPressed += OptionsButtonPressed;
            ConnectButton = VBox.GetChild<Button>("ConnectButton");
            ConnectButton.OnPressed += ConnectButtonPressed;
            VBox.GetChild<LineEdit>("IPBox").OnTextEntered += IPBoxEntered;

            _client.RunLevelChanged += RunLevelChanged;

            OptionsMenu = new OptionsMenu()
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

        private void ExitButtonPressed(BaseButton.ButtonEventArgs args)
        {
            IoCManager.Resolve<IGameControllerProxy>().GameController.Shutdown();
        }

        private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
        {
            OptionsMenu.OpenCentered();
        }

        private void ConnectButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var input = _mainMenuControl.GetChild("VBoxContainer").GetChild<LineEdit>("IPBox");
            TryConnect(input.Text);
        }

        private void IPBoxEntered(LineEdit.LineEditEventArgs args)
        {
            TryConnect(args.Text);
        }

        private void TryConnect(string address)
        {
            ConnectButton.Disabled = true;
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
                ConnectButton.Disabled = false;
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
        }

        private class MainMenuControl : Control
        {
            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/MainMenu/MainMenu.tscn");

            protected override void Initialize()
            {
                base.Initialize();

                if (GameController.OnGodot)
                {
                    return;
                }

                GetChild<TextureRect>("VBoxContainer/Logo").Texture = IoCManager.Resolve<IResourceCache>()
                    .GetResource<TextureResource>("/Textures/Logo/logo.png");

                GetChild("VBoxContainer").StyleIdentifier = "mainMenuVBox";
            }
        }
    }
}
