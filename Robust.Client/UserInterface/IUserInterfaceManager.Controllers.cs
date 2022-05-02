using Robust.Shared.Timing;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public T GetUIController<T>() where T : UIController, new();
}
