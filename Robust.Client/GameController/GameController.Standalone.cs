using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using Robust.Client.Interfaces;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client
{
    internal partial class GameController
    {
        private GameLoop _mainLoop;

        [Dependency] private IGameTiming _gameTiming;

        public static void Main()
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif

            IoCManager.InitThread();

            DisplayMode mode;
            if (Environment.GetCommandLineArgs().Contains("--headless"))
            {
                mode = DisplayMode.Headless;
            }
            else
            {
                mode = DisplayMode.Clyde;
            }

            ThreadUtility.MainThread = Thread.CurrentThread;
            InitIoC(mode);

            var gc = (GameController) IoCManager.Resolve<IGameController>();
            gc.Startup();
            gc.MainLoop(mode);

            Logger.Debug("Goodbye");
            IoCManager.Clear();
        }

        private void MainLoop(DisplayMode mode)
        {
            _mainLoop = new GameLoop(_gameTiming)
            {
                SleepMode = mode == DisplayMode.Headless ? SleepMode.Delay : SleepMode.None
            };

            _mainLoop.Tick += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Update(args.DeltaSeconds);
                }
            };

            _mainLoop.Render += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _gameTiming.CurFrame++;
                    _clyde.Render(new FrameEventArgs(args.DeltaSeconds));
                }
            };
            _mainLoop.Input += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _clyde.ProcessInput(new FrameEventArgs(args.DeltaSeconds));
                }
            };

            _mainLoop.Update += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _frameProcessMain(args.DeltaSeconds);
                }
            };

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}

