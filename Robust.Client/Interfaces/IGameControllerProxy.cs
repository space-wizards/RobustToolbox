namespace Robust.Client.Interfaces
{
    public interface IGameControllerProxy
    {
        IGameController GameController { get; }
    }

    internal interface IGameControllerProxyInternal : IGameControllerProxy
    {
        new IGameControllerInternal GameController { get; }
    }
}
