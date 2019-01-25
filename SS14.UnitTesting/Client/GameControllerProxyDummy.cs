using SS14.Client.Interfaces;

namespace SS14.UnitTesting.Client
{
    internal class GameControllerProxyDummy : IGameControllerProxyInternal
    {
        IGameController IGameControllerProxy.GameController => GameController;

        public IGameControllerInternal GameController =>
            throw new System.NotSupportedException("There is no GameController during unit tests. Sorry.");
    }
}
