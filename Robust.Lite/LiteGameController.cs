using System;
using System.Globalization;
using Robust.Client;
using Robust.Client.Input;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Lite
{
    internal class LiteGameController : IGameControllerInternal
    {
        private IGameLoop _mainLoop;

#pragma warning disable 649
        [Dependency] private readonly IClipboardManagerInternal _clipboardManager;
        [Dependency] private readonly IClydeInternal _clyde;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IFontManagerInternal _fontManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly ILogManager _logManager;
        [Dependency] private readonly IResourceCacheInternal _resourceCache;
        [Dependency] private readonly ISignalHandler _signalHandler;
        [Dependency] private readonly ITaskManager _taskManager;
        [Dependency] private readonly ITimerManager _timerManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;
#pragma warning restore 649

        public bool LoadConfigAndUserData { get; set; }
        public string ContentRootDir { get; set; }


        public void Shutdown(string reason = null)
        {
            _mainLoop.Running = false;
        }

        public void Startup()
        {
            throw new NotSupportedException();
        }

        public void Startup(InitialWindowParameters windowParameters)
        {
            _logManager.RootSawmill.AddHandler(new ConsoleLogHandler());

            _taskManager.Initialize();

            _signalHandler.MaybeStart();

            _taskManager.Initialize();

            // TODO: Init user data maybe?
            _resourceCache.Initialize(null);

#if FULL_RELEASE
            _resourceCache.MountContentDirectory(@"Resources/");
#else
            _resourceCache.MountContentDirectory($@"../../RobustToolbox/Resources");
            _resourceCache.MountContentDirectory($@"../../Resources");
#endif

            _localizationManager.LoadCulture(CultureInfo.CurrentCulture);

            if (windowParameters?.Size != null)
            {
                var (w, h) = windowParameters.Size.Value;
                _configurationManager.SetCVar("display.width", w);
                _configurationManager.SetCVar("display.height", h);
            }

            _clyde.Initialize(true);
            if (windowParameters?.WindowTitle != null)
            {
                _clyde.SetWindowTitle(_localizationManager.GetString(windowParameters.WindowTitle));
            }

            _fontManager.Initialize();
            _clipboardManager.Initialize();

            _eyeManager.Initialize();

            _userInterfaceManager.Initialize();

            _clyde.Ready();
        }

        public void MainLoop(GameController.DisplayMode mode)
        {
            _mainLoop = new GameLoop(_gameTiming);

            _mainLoop.Tick += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    ProcessUpdate(args);
                }
            };

            _mainLoop.Render += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _gameTiming.CurFrame++;
                    _clyde.Render();
                }
            };
            _mainLoop.Input += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _clyde.ProcessInput(args);
                }
            };

            _mainLoop.Update += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    RenderFrameProcess(args);
                }
            };

            _mainLoop.Run();
        }

        private void RenderFrameProcess(FrameEventArgs frameEventArgs)
        {
            _userInterfaceManager.FrameUpdate(frameEventArgs);
            _clyde.FrameProcess(frameEventArgs);
        }

        private void ProcessUpdate(FrameEventArgs frameEventArgs)
        {
            _timerManager.UpdateTimers(frameEventArgs);
            _taskManager.ProcessPendingTasks();
            _userInterfaceManager.Update(frameEventArgs);
        }

        public void KeyDown(KeyEventArgs keyEvent)
        {
            _userInterfaceManager.KeyDown(keyEvent);
        }

        public void KeyUp(KeyEventArgs keyEvent)
        {
            _userInterfaceManager.KeyUp(keyEvent);
        }

        public void TextEntered(TextEventArgs textEvent)
        {
            _userInterfaceManager.TextEntered(textEvent);
        }

        public void MouseDown(MouseButtonEventArgs mouseEvent)
        {
            _userInterfaceManager.MouseDown(mouseEvent);
        }

        public void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
            _userInterfaceManager.MouseUp(mouseButtonEventArgs);
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _userInterfaceManager.MouseMove(mouseMoveEventArgs);
        }

        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
            _userInterfaceManager.MouseWheel(mouseWheelEventArgs);
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            throw new NotSupportedException();
        }
    }
}
