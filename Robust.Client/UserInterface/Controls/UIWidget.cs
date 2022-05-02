using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public abstract class UIWidget : BoxContainer
{
    protected UIWidget()
    {
        IoCManager.InjectDependencies(this);
    }
}
