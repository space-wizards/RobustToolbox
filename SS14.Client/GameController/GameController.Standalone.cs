using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Timing;

namespace SS14.Client
{
    internal partial class GameController
    {
        private GameLoop _mainLoop;

        [Dependency] private IGameTiming _gameTimingHeadless;

        public static void Main()
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif

            IoCManager.Register<ISceneTreeHolder, SceneTreeHolder>();
            IoCManager.BuildGraph();

            if (Environment.GetCommandLineArgs().Contains("--headless"))
            {
                Mode = DisplayMode.Headless;
            }
            else
            {
                Mode = DisplayMode.OpenGL;
            }

            var gc = new GameController();
            gc.Startup();
            gc.MainLoop();
        }


        private void MainLoop()
        {
            _mainLoop = new GameLoop(_gameTimingHeadless)
            {
                SleepMode = Mode == DisplayMode.Headless ? SleepMode.Delay : SleepMode.None
            };

            /*
            var start = DateTime.Now;
            var frames = 0;
            */

            _mainLoop.Tick += (sender, args) => Update(args.DeltaSeconds);
            if (Mode == DisplayMode.OpenGL)
            {
                _mainLoop.Render += (sender, args) =>
                {
                    /*
                    frames++;
                    if ((DateTime.Now - start).TotalSeconds >= 1)
                    {
                        Logger.Info(frames.ToString());
                        start = DateTime.Now;
                        frames = 0;
                    }
                    */
                    _displayManagerOpenGL.Render(new FrameEventArgs(args.DeltaSeconds));
                };
                _mainLoop.Input += (sender, args) => _displayManagerOpenGL.ProcessInput(new FrameEventArgs(args.DeltaSeconds));
            }

            _mainLoop.Update += (sender, args) =>
            {
                _frameProcessMain(args.DeltaSeconds);
            };

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}

