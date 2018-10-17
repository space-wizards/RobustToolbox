using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Timing;

#if NOGODOT
namespace SS14.Client
{
    public partial class GameController
    {
        private GameLoop _mainLoop;

        [Dependency] private IGameTiming _gameTiming;

        public static void Main()
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif

            var gc = new GameController();
            gc.Startup();
            gc.MainLoop();
        }

        public ICollection<string> GetCommandLineArgs()
        {
            return Environment.GetCommandLineArgs();
        }

        private void MainLoop()
        {
            _mainLoop = new GameLoop(_gameTiming)
            {
                SleepMode = SleepMode.Delay
            };

            _mainLoop.Tick += (sender, args) => Update(args.DeltaSeconds);

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
        }
    }
}
#endif
