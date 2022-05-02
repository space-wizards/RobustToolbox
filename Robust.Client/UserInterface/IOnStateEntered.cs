namespace Robust.Client.UserInterface;

public interface IOnStateEntered<T> where T : State.State
{
    void OnStateEntered(T state);
}
