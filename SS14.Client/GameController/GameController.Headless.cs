using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
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
                SleepMode = SleepMode.Delay
            };

            _mainLoop.Tick += (sender, args) => Update(args.DeltaSeconds);
            if (Mode == DisplayMode.OpenGL)
            {
                _mainLoop.Render += (sender, args) => _displayManagerOpenGL.Render(new FrameEventArgs(args.DeltaSeconds));
                _mainLoop.Input += (sender, args) => _displayManagerOpenGL.ProcessInput(new FrameEventArgs(args.DeltaSeconds));
            }
            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}
