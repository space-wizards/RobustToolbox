namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Interface implemented by <see cref="UIController"/>s
/// </summary>
/// <typeparam name="T">The state type</typeparam>
public interface IOnStateExited<T> where T : State.State
{
    /// <summary>
    ///     Called by <see cref="UserInterfaceManager.OnStateChanged"/>
    ///     on <see cref="UIController"/>s that implement this method when a state
    ///     of the specified type is exited
    /// </summary>
    /// <param name="state">The state that was exited</param>
    void OnStateExited(T state);
}
