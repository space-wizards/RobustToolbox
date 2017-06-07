using SFML.Graphics;
using SFML.Window;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Shared.Utility;
using SS14.Shared.Prototypes;
using SS14.Shared.ContentLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using KeyArgs = SFML.Window.KeyEventArgs;

namespace SS14.Client
{
    public class GameController
    {

        #region Fields

        readonly private IPlayerConfigurationManager _configurationManager;
      //  private Input _input;
        readonly private INetworkGrapher _netGrapher;
        readonly private INetworkManager _networkManager;
        readonly private IStateManager _stateManager;
        readonly private IUserInterfaceManager _userInterfaceManager;
        readonly private IResourceManager _resourceManager;

        private SFML.System.Clock _clock;

        #endregion

        #region Properties

        #endregion

        #region Methods

        #region Constructors

        public GameController()
        {
            LogManager.Log("Initialising GameController.", LogLevel.Debug);

            ShowSplashScreen();

            LoadAssemblies();

            _configurationManager = IoCManager.Resolve<IPlayerConfigurationManager>();
            _configurationManager.Initialize(PathHelpers.ExecutableRelativeFile("player_config.xml"));

            _resourceManager = IoCManager.Resolve<IResourceManager>();

            _resourceManager.LoadBaseResources();
            _resourceManager.LoadLocalResources();

            //Setup Cluwne first, as the rest depends on it.
            SetupCluwne();
            CleanupSplashScreen();

            //Initialization of private members
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(PathHelpers.ExecutableRelativeFile("prototypes"));
            prototypeManager.Resync();
            _networkManager = IoCManager.Resolve<INetworkManager>();
            _netGrapher = IoCManager.Resolve<INetworkGrapher>();
            _stateManager = IoCManager.Resolve<IStateManager>();
            _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

            _stateManager.RequestStateChange<MainScreen> ();

            FrameEventArgs _frameEvent;
            // EventArgs _frameEventArgs;
            _clock = new SFML.System.Clock();

            while (CluwneLib.IsRunning == true)
            {
                var lastFrameTime = _clock.ElapsedTime.AsSeconds();
                _clock.Restart();
                _frameEvent = new FrameEventArgs(lastFrameTime);
                CluwneLib.ClearCurrentRendertarget(Color.Black);
                CluwneLib.Screen.DispatchEvents();
                CluwneLib.RunIdle (this, _frameEvent);
                CluwneLib.Screen.Display();
            }
            _networkManager.Disconnect();
            CluwneLib.Terminate();
            LogManager.Log("GameController terminated.");
        }

        private void LoadAssemblies()
        {
            var assemblies = new List<Assembly>(2);
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"));
            assemblies.Add(Assembly.GetExecutingAssembly());
            IoCManager.AddAssemblies(assemblies);

            assemblies.Clear();

            // So we can't actually access this until IoC has loaded the initial assemblies. Yay.
            var loader = IoCManager.Resolve<IContentLoader>();
            assemblies.Clear();

            // TODO this should be done on connect.
            // The issue is that due to our giant trucks of shit code.
            // It'd be extremely hard to integrate correctly.
            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Shared.Content.dll");
                loader.LoadAssembly(contentAssembly);
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the shared content assembly (SS14.Shared.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Server.Content.dll");
                loader.LoadAssembly(contentAssembly);
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the server content assembly (SS14.Server.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            IoCManager.AddAssemblies(assemblies);
        }

        private void ShowSplashScreen()
        {
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
            logo.Position = new Vector2f(SIZE_X/2 - logoSize.X/2,SIZE_Y/2 - logoSize.Y/2);

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
        }

        private void CleanupSplashScreen()
        {
            CluwneLib.CleanupSplashScreen();
        }


        #endregion

        #region EventHandlers

        private void CluwneLibIdle(object sender, FrameEventArgs e)
        {
            _networkManager.UpdateNetwork();
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
            if(_stateManager!=null)
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

        #endregion

        #endregion

        #region Privates

        bool onetime = true;

        private void SetupCluwne()
        {
            uint displayWidth  = _configurationManager.GetDisplayWidth();
            uint displayHeight = _configurationManager.GetDisplayHeight();
            bool isFullscreen  = _configurationManager.GetFullscreen();
            var refresh        = _configurationManager.GetDisplayRefresh();

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
            CluwneLib.Screen.BackgroundColor      = Color.Black;
            CluwneLib.Screen.Resized             += MainWindowResizeEnd;
            CluwneLib.Screen.Closed              += MainWindowRequestClose;
            CluwneLib.Screen.KeyPressed          += KeyDownEvent;
            CluwneLib.Screen.KeyReleased         += KeyUpEvent;
            CluwneLib.Screen.MouseButtonPressed  += MouseDownEvent;
            CluwneLib.Screen.MouseButtonReleased += MouseUpEvent;
            CluwneLib.Screen.MouseMoved          += MouseMoveEvent;
            CluwneLib.Screen.MouseWheelMoved     += MouseWheelMoveEvent;
            CluwneLib.Screen.MouseEntered        += MouseEntered;
            CluwneLib.Screen.MouseLeft           += MouseLeft;
            CluwneLib.Screen.TextEntered         += TextEntered;

            CluwneLib.Go();
            IoCManager.Resolve<IKeyBindingManager>().Initialize();
        }

        #endregion

        #endregion
    }
}

