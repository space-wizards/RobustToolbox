using Robust.Client.Interfaces;

namespace Robust.Client
{
    internal sealed partial class GameController
    {
        // Since GameController isn't managed by IoC,
        // this'll have to do as a proxy to it.
        // Should rarely be needed anyways.
        // ReSharper disable once ClassNeverInstantiated.Local
        private class GameControllerProxy : IGameControllerProxyInternal
        {
            public GameController GameController;

            IGameController IGameControllerProxy.GameController => GameController;
            IGameControllerInternal IGameControllerProxyInternal.GameController => GameController;
        }
    }
}
