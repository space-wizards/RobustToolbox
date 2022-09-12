using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Interface implemented by <see cref="UIController"/>s
/// </summary>
/// <typeparam name="T">The entity system type</typeparam>
public interface IOnSystemLoaded<T> where T : IEntitySystem
{
    /// <summary>
    ///     Called by <see cref="UserInterfaceManager.OnSystemLoaded"/>
    ///     on <see cref="UIController"/>s that implement this method when a system
    ///     of the specified type is loaded
    /// </summary>
    /// <param name="system">The system that was loaded</param>
    void OnSystemLoaded(T system);
}
