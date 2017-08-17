using OpenTK;
using SFML.Graphics;
using SFML.Window;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Configuration;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Prototypes;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using KeyArgs = SFML.Window.KeyEventArgs;

namespace SS14.Client
{
    public class GameController : IGameController
    {
        #region Fields

        [Dependency]
        readonly private IConfigurationManager _configurationManager;
        [Dependency]
        readonly private INetworkGrapher _netGrapher;
        [Dependency]
        readonly private IClientNetManager _networkManager;
        [Dependency]
        readonly private IStateManager _stateManager;
        [Dependency]
        readonly private IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly private IResourceCache _resourceCache;
        [Dependency]
        readonly private ITileDefinitionManager _tileDefinitionManager;
        [Dependency]
        readonly private ISS14Serializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resourceManager;
        [Dependency]
        private readonly IMapManager _mapManager;

        #endregion Fields

        #region Methods

        #region Constructors

        private TimeSpan _lastTick;
        private TimeSpan _lastKeepUpAnnounce;

        public void Run()
        {
            Logger.Debug("Initializing GameController.");

            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            _resourceCache.LoadBaseResources();
            // Load resources used by splash screen and main menu.
            LoadSplashResources();
            ShowSplashScreen();

            _resourceCache.LoadLocalResources();

            LoadContentAssembly<GameShared>("Shared");
            LoadContentAssembly<GameClient>("Client");

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            //Setup Cluwne first, as the rest depends on it.
            SetupCluwne();
            CleanupSplashScreen();

            //Initialization of private members
            _tileDefinitionManager.InitializeResources();

            _serializer.Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(PathHelpers.ExecutableRelativeFile("Resources/Prototypes"));
            prototypeManager.Resync();
            _networkManager.Initialize(false);
            _netGrapher.Initialize();
            _userInterfaceManager.Initialize();
            _mapManager.Initialize();

            _stateManager.RequestStateChange<MainScreen>();

            #region GameLoop

            // maximum number of ticks to queue before the loop slows down.
            const int maxTicks = 5;

            _time.ResetRealTime();
            var maxTime = TimeSpan.FromTicks(_time.TickPeriod.Ticks * maxTicks);

            while (CluwneLib.IsRunning)
            {
                var accumulator = _time.RealTime - _lastTick;

                // If the game can't keep up, limit time.
                if (accumulator > maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = maxTime;

                    // pull lastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _lastTick = _time.RealTime - maxTime;

                    // announce we are falling behind
                    if ((_time.RealTime - _lastKeepUpAnnounce).TotalSeconds >= 15.0)
                    {
                        Logger.Warning("[SRV] MainLoop: Cannot keep up!");
                        _lastKeepUpAnnounce = _time.RealTime;
                    }
                }

                _time.StartFrame();

                var realFrameEvent = new FrameEventArgs((float)_time.RealFrameTime.TotalSeconds);

                // process Net/KB/Mouse input
                Process(realFrameEvent);

                _time.InSimulation = true;
                // run the simulation for every accumulated tick
                while (accumulator >= _time.TickPeriod)
                {
                    accumulator -= _time.TickPeriod;
                    _lastTick += _time.TickPeriod;

                    // only run the sim if unpaused, but still use up the accumulated time
                    if (!_time.Paused)
                    {
                        // update the simulation
                        var simFrameEvent = new FrameEventArgs((float) _time.FrameTime.TotalSeconds);
                        Update(simFrameEvent);
                        _time.CurTick++;
                    }
                }

                // if not paused, save how close to the next tick we are so interpolation works
                if (!_time.Paused)
                    _time.TickRemainder = accumulator;

                _time.InSimulation = false;

                // render the simulation
                Render(realFrameEvent);
            }

            #endregion

            _networkManager.ClientDisconnect("Client disconnected from game.");
            CluwneLib.Terminate();
            Logger.Info("GameController terminated.");

            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
        }

        private void LoadContentAssembly<T>(string name) where T: GameShared
        {
            // get the assembly from the file system
            if (_resourceManager.TryContentFileRead($@"Assemblies/Content.{name}.dll", out MemoryStream gameDll))
            {
                Logger.Debug($"[SRV] Loading {name} Content DLL");

                // see if debug info is present
                if (_resourceManager.TryContentFileRead($@"Assemblies/Content.{name}.pdb", out MemoryStream gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
                else
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
            }
            else
            {
                Logger.Warning($"[ENG] Could not find {name} Content DLL");
            }
        }

        /// <summary>
        /// Processes all simulation I/O. Keyboard/Mouse/Network code gets called here.
        /// </summary>
        private void Process(FrameEventArgs e)
        {
            //TODO: Keyboard/Mouse input needs to be processed here.

        }

        /// <summary>
        /// Runs a tick of the simulation.
        /// </summary>
        /// <param name="e">Current GameTiming.FrameTime</param>
        private void Update(FrameEventArgs e)
        {
            _networkManager.ProcessPackets();
            CluwneLib.RunIdle(this, e);
            _stateManager.Update(e);
        }

        /// <summary>
        /// Renders the view of the simulation.
        /// </summary>
        /// <param name="e">Current GameTiming.RealFrameTime</param>
        private void Render(FrameEventArgs e)
        {
            CluwneLib.ClearCurrentRendertarget(Color.Black);
            CluwneLib.Screen.DispatchEvents();

            // draw everything
            _stateManager.Render(e);

            // interface runs in realtime, so it is updated here
            _userInterfaceManager.Update(e);

            _userInterfaceManager.Render(e);

            _netGrapher.Update();

            // swap buffers to show the screen
            CluwneLib.Screen.Display();
        }

        private void LoadSplashResources()
        {
            var logoTexture = _resourceCache.LoadTextureFrom("ss14_logo", _resourceManager.ContentFileRead(@"Textures/Logo/logo.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo", logoTexture);

            var backgroundTexture = _resourceCache.LoadTextureFrom("ss14_logo_background", _resourceManager.ContentFileRead(@"Textures/Logo/background.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo_background", backgroundTexture);

            var nanotrasenTexture = _resourceCache.LoadTextureFrom("ss14_logo_nt", _resourceManager.ContentFileRead(@"Textures/Logo/nanotrasen.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo_nt", nanotrasenTexture);
        }

        private void ShowSplashScreen()
        {
            // Do nothing when we're on DEBUG builds.
            // The splash is just annoying.
#if !DEBUG
            const uint SIZE_X = 600;
            const uint SIZE_Y = 300;
            // Size of the NT logo in the bottom right.
            const float NT_SIZE_X = SIZE_X / 10f;
            const float NT_SIZE_Y = SIZE_Y / 10f;
            CluwneWindow window = CluwneLib.ShowSplashScreen(new VideoMode(SIZE_X, SIZE_Y));

            var logo = _resourceCache.GetSprite("ss14_logo");
            logo.Position = new Vector2f(SIZE_X / 2 - logo.TextureRect.Width / 2, SIZE_Y / 2 - logo.TextureRect.Height / 2);

            var background = _resourceCache.GetSprite("ss14_logo_background");
            background.Scale = new Vector2f((float)SIZE_X / background.TextureRect.Width, (float)SIZE_Y / background.TextureRect.Height);

            var nanotrasen = _resourceCache.GetSprite("ss14_logo_nt");
            nanotrasen.Scale = new Vector2f(NT_SIZE_X / nanotrasen.TextureRect.Width, NT_SIZE_Y / nanotrasen.TextureRect.Height);
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
