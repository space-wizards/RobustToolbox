using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
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
        [Dependency]
        readonly IBaseClient _client;
        [Dependency]
        readonly IUserInterfaceManager userInterfaceManager;

        private Control MainMenuControl;

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="managers">A dictionary of common managers from the IOC system, so you don't have to resolve them yourself.</param>
        public MainScreen(IDictionary<Type, object> managers) : base(managers) { }

        /// <inheritdoc />
        public override void InitializeGUI()
        {
            IoCManager.InjectDependencies(this);

            var scene = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/MainMenu/MainMenu.tscn");
            MainMenuControl = Control.InstanceScene(scene);

            userInterfaceManager.StateRoot.AddChild(MainMenuControl);

            var VBox = MainMenuControl.GetChild("VBoxContainer");
            VBox.GetChild<Button>("ExitButton").OnPressed += ExitButtonPressed;
            VBox.GetChild<Button>("OptionsButton").OnPressed += OptionsButtonPressed;
            VBox.GetChild<Button>("ConnectButton").OnPressed += ConnectButtonPressed;
            VBox.GetChild<LineEdit>("IPBox").OnTextEntered += IPBoxEntered;
        }

        /// <inheritdoc />
        public override void Startup()
        {
            _client.RunLevelChanged += RunLevelChanged;
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _client.RunLevelChanged -= RunLevelChanged;

            userInterfaceManager.StateRoot.RemoveChild(MainMenuControl);
            MainMenuControl.Dispose();
        }

        private void ExitButtonPressed(BaseButton.ButtonEventArgs args)
        {
            IoCManager.Resolve<IGameControllerProxy>().GameController.Shutdown();
        }

        private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
        {
            userInterfaceManager.Popup("Sorry, options menu's not implemented yet!", "// TODO:");
        }

        private void ConnectButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var input = MainMenuControl.GetChild("VBoxContainer").GetChild<LineEdit>("IPBox");
            TryConnect(input.Text);
        }

        private void IPBoxEntered(LineEdit.LineEditEventArgs args)
        {
            TryConnect(args.Text);
        }

        private void TryConnect(string address)
        {
            try
            {
                ParseAddress(address, out var ip, out var port);
                _client.ConnectToServer(ip, port);
            }
            catch (ArgumentException e)
            {
                userInterfaceManager.Popup($"Unable to resolve address: {e.Message}", "Invalid IP");
            }
        }

        private void RunLevelChanged(object obj, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Lobby)
            {
                StateManager.RequestStateChange<Lobby>();
            }
        }

        private void ParseAddress(string address, out string ip, out ushort port)
        {
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
    }
}
