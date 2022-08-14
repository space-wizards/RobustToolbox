using System;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public T CreatePopup<T>() where T : Popup, new();
    public bool RemoveFirstPopup<T>() where T : Popup, new();
    public bool TryGetFirstPopup<T>(out T? popup) where T : Popup, new();
    public bool TryGetFirstPopup(Type type, out Popup? popup);

    public bool RemoveFirstWindow<T>() where T : BaseWindow, new();
    public T CreateWindow<T>() where T : BaseWindow, new();

    public void ClearWindows();
    public T GetFirstWindow<T>() where T : BaseWindow, new();
    public bool TryGetFirstWindow<T>(out T? window) where T : BaseWindow, new();
    public bool TryGetFirstWindow(Type type, out BaseWindow? window);
}
