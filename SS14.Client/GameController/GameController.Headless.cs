using System;
using System.Collections.Generic;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Timing;

namespace SS14.Client
{
    public partial class GameController
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

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}
