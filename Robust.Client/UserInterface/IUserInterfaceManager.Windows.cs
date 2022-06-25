using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{

    public bool RemoveNamedPopup(string popupName);
    public bool RegisterNamedPopup(string popupName, Popup popup);
    public bool TryGetNamedPopup(string popupName, out Popup? popup);
    public Popup GetNamedPopup(string popupName);
    public T CreatePopupOfType<T>() where T : Popup, new();
    public T? CreateNamedPopup<T>(string popupName, Vector2 position) where T : Popup, new();
    public bool RemoveFirstPopupOfType<T>() where T : Popup, new();
    public bool TryGetFirstPopupByType<T>(out T? popup) where T : Popup, new();
    public bool TryGetFirstPopupByType(Type type, out Popup? popup);

    public bool TryGetPopupWindow(string windowName, out WindowRoot? window);
    public bool RemovePopupWindow(string windowName);
    public WindowRoot GetPopupWindow(string windowName);
    public WindowRoot? CreatePopupWindow(string windowName, string? displayName = null, int width = 1000,
        int height = 1000, Action<WindowRequestClosedEventArgs>? onClosed = null);
    public bool RegisterNamedWindow(string name, BaseWindow window);
    public BaseWindow GetNamedWindow(string name);
    public bool TryGetNamedWindow(string name, out BaseWindow? window);
    public T? CreateNamedWindow<T>(string name) where T : BaseWindow, new();
    public bool RemoveFirstWindowOfType<T>() where T : BaseWindow, new();
    public T CreateWindowOfType<T>() where T : BaseWindow, new();
    public bool RemoveNamedWindow(string name);
    public T GetFirstWindowOfType<T>() where T : BaseWindow, new();
    public bool TryGetFirstWindowByType<T>(out T? window) where T : BaseWindow, new();
    public bool TryGetFirstWindowByType(Type type, out BaseWindow? window);
}
