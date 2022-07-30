using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface;

//Notices your UIController, *UwU Whats this?*
/// <summary>
///     Each <see cref="UIController"/> is instantiated as a singleton by <see cref="UserInterfaceManager"/>
///     <see cref="UIController"/> can use <see cref="DependencyAttribute"/> for regular IoC dependencies
///     and <see cref="UISystemDependency"/> to depend on <see cref="EntitySystem"/>s, which will be automatically
///     injected once they are created.
/// </summary>
public abstract class UIController
{
    [Dependency] protected readonly IUserInterfaceManager UIManager = default!;

    public virtual void FrameUpdate(FrameEventArgs args)
    {
    }

    public virtual void Initialize()
    {
    }

    // TODO HUD REFACTOR BEFORE MERGE make these two methods less ass to use
    public virtual void OnSystemLoaded(IEntitySystem system)
    {
    }

    public virtual void OnSystemUnloaded(IEntitySystem system)
    {
    }
}
