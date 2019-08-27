using System;
using System.Linq;
using CommandLine;
using Robust.Client.Interfaces;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client
{
    internal partial class GameController
    {
        private IGameLoop _mainLoop;

#pragma warning disable 649
        [Dependency] private IGameTiming _gameTiming;
#pragma warning restore 649

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineArgs>(args)
                .WithParsed(ParsedMain);
        }

        private static void ParsedMain(CommandLineArgs args)
        {
            IoCManager.InitThread();

            var mode = args.Headless ? DisplayMode.Headless : DisplayMode.Clyde;

            InitIoC(mode);

            var gc = (GameController) IoCManager.Resolve<IGameController>();
            gc.SetCommandLineArgs(args);
            gc.Startup();
            gc.MainLoop(mode);

            Logger.Debug("Goodbye");
            IoCManager.Clear();
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }

        public void MainLoop(DisplayMode mode)
        {
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(_gameTiming)
                {
                    SleepMode = mode == DisplayMode.Headless ? SleepMode.Delay : SleepMode.None
                };
            }

            _mainLoop.Tick += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Update(args);
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
                    _frameProcessMain(args);
                }
            };

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}
