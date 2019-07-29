using System;
using Robust.Client;
using Robust.Client.Interfaces;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;

namespace Robust.Lite
{
    public static class LiteLoader
    {
        public static void Run(Action postInit, InitialWindowParameters windowParameters=null)
        {
            IoCManager.InitThread();

            ClientIoC.RegisterIoC(GameController.DisplayMode.Clyde);
            IoCManager.Register<IGameController, LiteGameController>(true);
            IoCManager.Register<IGameControllerInternal, LiteGameController>(true);
            IoCManager.Register<LiteGameController, LiteGameController>(true);
            IoCManager.BuildGraph();

            var gc = IoCManager.Resolve<LiteGameController>();
            gc.Startup(windowParameters);

            IoCManager.Resolve<ITaskManager>().RunOnMainThread(postInit);
            gc.MainLoop(GameController.DisplayMode.Clyde);

            IoCManager.Clear();
        }
    }
}
