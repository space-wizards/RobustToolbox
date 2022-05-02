using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public abstract class UIWidget : BoxContainer
{
    [Dependency] protected readonly IUIControllerManager UIControllerManager = default!;

    protected UIWidget()
    {
        IoCManager.InjectDependencies(this);
    }
}
