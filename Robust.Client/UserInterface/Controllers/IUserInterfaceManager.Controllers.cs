using Robust.Client.UserInterface.Controllers;

// ReSharper disable once CheckNamespace
namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public T GetUIController<T>() where T : UIController, new();
}
