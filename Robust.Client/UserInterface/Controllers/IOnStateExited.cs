namespace Robust.Client.UserInterface.Controllers;

public interface IOnStateExited<T> where T : State.State
{
    void OnStateExited(T state);
}
