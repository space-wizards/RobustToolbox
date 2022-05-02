namespace Robust.Client.UserInterface;

public interface IOnStateExited<T> where T : State.State
{
    void OnStateExited(T state);
}
