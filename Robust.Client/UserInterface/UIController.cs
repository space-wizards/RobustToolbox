using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface;

// Notices your UIController, *UwU Whats this?*
/// <summary>
///     Each <see cref="UIController"/> is instantiated as a singleton by <see cref="UserInterfaceManager"/>
///     <see cref="UIController"/> can use <see cref="DependencyAttribute"/> for regular IoC dependencies
///     and <see cref="UISystemDependencyAttribute"/> to depend on <see cref="EntitySystem"/>s, which will be automatically
///     injected once they are created.
/// </summary>
public abstract class UIController
{
    [Dependency] protected readonly IUserInterfaceManager UIManager = default!;

    public virtual void Initialize()
    {
    }

    public virtual void FrameUpdate(FrameEventArgs args)
    {
    }

    // TODO HUD REFACTOR BEFORE MERGE make these two methods less ass to use
    /// <summary>
    ///     Called when a system is loaded, if this <see cref="UIController"/> depends on it with
    ///     the <see cref="UISystemDependencyAttribute"/> attribute
    /// </summary>
    /// <param name="system">The system that was loaded</param>
    public virtual void OnSystemLoaded(IEntitySystem system)
    {
    }

    /// <summary>
    ///     Called when a system is unloaded, if this <see cref="UIController"/> depends on it with
    ///     the <see cref="UISystemDependencyAttribute"/> attribute
    /// </summary>
    /// <param name="system">The system that was unloaded</param>
    public virtual void OnSystemUnloaded(IEntitySystem system)
    {
    }
}
