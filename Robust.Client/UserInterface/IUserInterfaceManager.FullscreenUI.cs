using Robust.Client.UserInterface.CustomControls;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public T CreateFullscreen<T>() where T : BaseFullscreen, new();
}
