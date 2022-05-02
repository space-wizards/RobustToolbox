using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{

    public bool TryGetPopupWindow(string windowName, out WindowRoot? window);
    public bool RemovePopupWindow(string windowName);
    public WindowRoot GetPopupWindow(string windowName);
    public WindowRoot? CreatePopupWindow(string windowName, string? displayName = null, int width = 1000,
        int height = 1000, Action<WindowRequestClosedEventArgs>? onClosed = null);
    public bool RegisterNamedWindow(string name, BaseWindow window);
    public T? CreateNamedWindow<T>(string name) where T : BaseWindow, new();
    public bool RemoveWindowOfType<T>() where T : BaseWindow, new();
    public T? CreateWindowOfType<T>() where T : BaseWindow, new();
    public bool RemoveNamedWindow(string name);
    public T GetFirstWindowOfType<T>() where T : BaseWindow, new();
    public bool TryGetWindowByType<T>(out T? window) where T : BaseWindow, new();
    public bool TryGetWindowByType(Type type, out BaseWindow? window);
}
