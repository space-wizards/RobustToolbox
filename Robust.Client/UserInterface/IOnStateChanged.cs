namespace Robust.Client.UserInterface;

public interface IOnStateChanged<T> where T : State.State
{
    void OnStateChanged(T state);
}
