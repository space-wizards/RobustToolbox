using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Interface implemented by <see cref="UIController"/>s
///     Implements both <see cref="IOnSystemLoaded{T}"/> and <see cref="IOnSystemUnloaded{T}"/>
/// </summary>
/// <typeparam name="T">The entity system type</typeparam>
public interface IOnSystemChanged<T> : IOnSystemLoaded<T>, IOnSystemUnloaded<T> where T : IEntitySystem
{
}
