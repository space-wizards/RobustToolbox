using SS14.Client.Interfaces;

namespace SS14.UnitTesting.Client
{
    public class GameControllerProxyDummy : IGameControllerProxy
    {
        public IGameController GameController => throw new System.NotSupportedException("There is no GameController during unit tests. Sorry.");
    }
}
