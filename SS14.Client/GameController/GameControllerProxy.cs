using SS14.Client.Interfaces;

namespace SS14.Client
{
    public sealed partial class GameController
    {
        // Since GameController isn't managed by IoC,
        // this'll have to do as a proxy to it.
        // Should rarely be needed anyways.
        class GameControllerProxy : IGameControllerProxy
        {
            public IGameController GameController { get; set; }
        }
    }
}
