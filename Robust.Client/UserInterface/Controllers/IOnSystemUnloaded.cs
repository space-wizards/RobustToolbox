using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Interface implemented by <see cref="UIController"/>s
/// </summary>
/// <typeparam name="T">The entity system type</typeparam>
public interface IOnSystemUnloaded<T> where T : IEntitySystem
{
    /// <summary>
    ///     Called by <see cref="UserInterfaceManager.OnSystemUnloaded"/>
    ///     on <see cref="UIController"/>s that implement this method when a system
    ///     of the specified type is unloaded
    /// </summary>
    /// <param name="system">The system that was unloaded</param>
    void OnSystemUnloaded(T system);
}
