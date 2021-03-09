namespace Robust.Client
{
    public interface IGameController
    {
        InitialLaunchState LaunchState { get; }

        void Shutdown(string? reason=null);
    }
}