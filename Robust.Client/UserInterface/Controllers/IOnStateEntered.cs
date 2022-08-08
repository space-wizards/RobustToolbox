namespace Robust.Client.UserInterface.Controllers;

public interface IOnStateEntered<T> where T : State.State
{
    void OnStateEntered(T state);
}
