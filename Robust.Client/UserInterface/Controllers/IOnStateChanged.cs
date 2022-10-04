namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Interface implemented by <see cref="UIController"/>s
///     Implements both <see cref="IOnStateEntered{T}"/> and <see cref="IOnStateExited{T}"/>
/// </summary>
/// <typeparam name="T">The state type</typeparam>
public interface IOnStateChanged<T> : IOnStateEntered<T>, IOnStateExited<T> where T : State.State
{
}
