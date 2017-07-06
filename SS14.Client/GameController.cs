using SFML.Graphics;
using SFML.Window;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.MessageLogging;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Configuration;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using SS14.Shared.Prototypes;
using System;
using System.Reflection;
using System.Windows.Forms;
using SS14.Shared.Interfaces.Network;
using KeyArgs = SFML.Window.KeyEventArgs;

namespace SS14.Client
{
    public class GameController : IGameController
    {
        #region Fields

        [Dependency]
        readonly private IConfigurationManager _configurationManager;
        //  private Input _input;
        [Dependency]
        readonly private INetworkGrapher _netGrapher;
        [Dependency]
        readonly private INetClientManager _networkManager;
        [Dependency]
        readonly private IStateManager _stateManager;
        [Dependency]
        readonly private IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly private IResourceManager _resourceManager;
        [Dependency]
        readonly private IMessageLogger _messageLogger;
        [Dependency]
        readonly private IEntityNetworkManager _entityNetworkManager;
        [Dependency]
        readonly private ITileDefinitionManager _tileDefinitionManager;

        #endregion Fields

        #region Methods

        #region Constructors
        public void Run()
        {
            Logger.Debug("Initializing GameController.");

            ShowSplashScreen();

            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            _resourceManager.LoadBaseResources();
            _resourceManager.LoadLocalResources();

            //Setup Cluwne first, as the rest depends on it.
            SetupCluwne();
            CleanupSplashScreen();

            //Initialization of private members
            _messageLogger.Initialize();
            _entityNetworkManager.Initialize();
            _tileDefinitionManager.InitializeResources();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(PathHelpers.ExecutableRelativeFile("Prototypes"));
            prototypeManager.Resync();
            _networkManager.Initialize(false);
            _netGrapher.Initialize();
            _userInterfaceManager.Initialize();

            _stateManager.RequestStateChange<MainScreen>();

            FrameEventArgs _frameEvent;
            // EventArgs _frameEventArgs;
            var _clock = new Clock();

            while (CluwneLib.IsRunning)
            {
                var lastFrameTime = _clock.ElapsedTime.AsSeconds();
                _clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();
                CluwneLib.RunIdle(this, _frameEvent);
                CluwneLib.Screen.Display();
            }
            _networkManager.ClientDisconnect("Client disconnected from game.");
            CluwneLib.Terminate();
            Logger.Info("GameController terminated.");

            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }

        private void ShowSplashScreen()
        {
            // Do nothing when we're on DEBUG builds.
            // The splash is just annoying.
#if !DEBUG
            const uint SIZE_X = 600;
            const uint SIZE_Y = 300;
            // Size of the NT logo in the bottom left.
            const float NT_SIZE_X = SIZE_X / 10f;
            const float NT_SIZE_Y = SIZE_Y / 10f;
            CluwneWindow window = CluwneLib.ShowSplashScreen(new VideoMode(SIZE_X, SIZE_Y));

            var assembly = Assembly.GetExecutingAssembly();

            var logoTexture = new Texture(assembly.GetManifestResourceStream("SS14.Client._EmbeddedBaseResources.Logo.logo.png"));
            var logo = new SFML.Graphics.Sprite(logoTexture);
            var logoSize = logoTexture.Size;
            logo.Position = new Vector2f(SIZE_X / 2 - logoSize.X / 2, SIZE_Y / 2 - logoSize.Y / 2);

            var backgroundTexture = new Texture(assembly.GetManifestResourceStream("SS14.Client._EmbeddedBaseResources.Logo.background.png"));
            var background = new SFML.Graphics.Sprite(backgroundTexture);
            var backgroundSize = backgroundTexture.Size;
            background.Scale = new Vector2f((float)SIZE_X / backgroundSize.X, (float)SIZE_Y / backgroundSize.Y);

            var nanotrasenTexture = new Texture(assembly.GetManifestResourceStream("SS14.Client._EmbeddedBaseResources.Logo.nanotrasen.png"));
            var nanotrasen = new SFML.Graphics.Sprite(nanotrasenTexture);
            var nanotrasenSize = nanotrasenTexture.Size;
            nanotrasen.Scale = new Vector2f(NT_SIZE_X / nanotrasenSize.X, NT_SIZE_Y / nanotrasenSize.Y);
            nanotrasen.Position = new Vector2f(SIZE_X - NT_SIZE_X - 5, SIZE_Y - NT_SIZE_Y - 5);
            nanotrasen.Color = new Color(255, 255, 255, 64);

            window.Draw(background);
            window.Draw(logo);
            window.Draw(nanotrasen);
            window.Display();
#endif
        }

        private void CleanupSplashScreen()
        {
            // Do nothing when we're on DEBUG builds.
            // The splash is just annoying.
#if !DEBUG
            CluwneLib.CleanupSplashScreen();
#endif
        }

        #endregion Constructors

        #region EventHandlers

        private void CluwneLibIdle(object sender, FrameEventArgs e)
        {
            _networkManager.ProcessPackets();
            _stateManager.Update(e);

            _userInterfaceManager.Update(e);
            _userInterfaceManager.Render(e);

            _netGrapher.Update();
        }

        private void MainWindowLoad(object sender, EventArgs e)
        {
            _stateManager.RequestStateChange<MainScreen>();
        }

        private void MainWindowResizeEnd(object sender, SizeEventArgs e)
        {
            var view = new SFML.Graphics.View(
                new SFML.System.Vector2f(e.Width / 2, e.Height / 2),
                new SFML.System.Vector2f(e.Width, e.Height)
                );
            CluwneLib.Screen.SetView(view);
            _stateManager.FormResize();
        }
        private void MainWindowRequestClose(object sender, EventArgs e)
        {
            CluwneLib.Stop();
        }

        #region Input Handling

        /// <summary>
        /// Handles any keydown events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The KeyArgsinstance containing the event data.</param>
        private void KeyDownEvent(object sender, KeyArgs e)
        {
            if (_stateManager != null)
                _stateManager.KeyDown(e);

            switch (e.Code)
            {
                case Keyboard.Key.F3:
                    IoCManager.Resolve<INetworkGrapher>().Toggle();
                    break;
            }
        }

        /// <summary>
        /// Handles any keyup events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The KeyArgs instance containing the event data.</param>
        private void KeyUpEvent(object sender, KeyArgs e)
        {
            if (_stateManager != null)
                _stateManager.KeyUp(e);
        }

        /// <summary>
        /// Handles mouse wheel input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseWheelEventArgs instance containing the event data.</param>
        private void MouseWheelMoveEvent(object sender, MouseWheelEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseWheelMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseMoveEventArgs instance containing the event data.</param>
        private void MouseMoveEvent(object sender, MouseMoveEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseButtonEventArgs instance containing the event data.</param>
        private void MouseDownEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseButtonEventArgs instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseUp(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void MouseEntered(object sender, EventArgs e)
        {
            Cursor.Hide();
            if (_stateManager != null)
                _stateManager.MouseEntered(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void MouseLeft(object sender, EventArgs e)
        {
            Cursor.Show();
            if (_stateManager != null)
                _stateManager.MouseLeft(e);
        }

        private void TextEntered(object sender, TextEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.TextEntered(e);
        }

        #endregion Input Handling

        #endregion EventHandlers

        #region Privates

        bool onetime = true;

        private void SetupCluwne()
        {
            _configurationManager.RegisterCVar("display.width", 1280, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.height", 720, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.fullscreen", false, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.refresh", 60, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.vsync", false, CVarFlags.ARCHIVE);

            uint displayWidth = (uint) _configurationManager.GetCVar<int>("display.width");
            uint displayHeight = (uint) _configurationManager.GetCVar<int>("display.height");
            bool isFullscreen = _configurationManager.GetCVar<bool>("display.fullscreen");
            uint refresh = (uint) _configurationManager.GetCVar<int>("display.refresh");

            CluwneLib.Video.SetFullscreen(isFullscreen);
            CluwneLib.Video.SetRefreshRate(refresh);
            CluwneLib.Video.SetWindowSize(displayWidth, displayHeight);
            CluwneLib.Initialize();
            if (onetime)
            {
                //every time the video settings change we close the old screen and create a new one
                //SetupCluwne Gets called to reset the event handlers to the new screen
                CluwneLib.FrameEvent += CluwneLibIdle;
                CluwneLib.RefreshVideoSettings += SetupCluwne;
                onetime = false;
            }
            CluwneLib.Screen.SetMouseCursorVisible(false);
            CluwneLib.Screen.BackgroundColor = Color.Black;
            CluwneLib.Screen.Resized += MainWindowResizeEnd;
            CluwneLib.Screen.Closed += MainWindowRequestClose;
            CluwneLib.Screen.KeyPressed += KeyDownEvent;
            CluwneLib.Screen.KeyReleased += KeyUpEvent;
            CluwneLib.Screen.MouseButtonPressed += MouseDownEvent;
            CluwneLib.Screen.MouseButtonReleased += MouseUpEvent;
            CluwneLib.Screen.MouseMoved += MouseMoveEvent;
            CluwneLib.Screen.MouseWheelMoved += MouseWheelMoveEvent;
            CluwneLib.Screen.MouseEntered += MouseEntered;
            CluwneLib.Screen.MouseLeft += MouseLeft;
            CluwneLib.Screen.TextEntered += TextEntered;

            CluwneLib.Go();
            IoCManager.Resolve<IKeyBindingManager>().Initialize();
        }

        #endregion Privates

        #endregion Methods
    }
}
