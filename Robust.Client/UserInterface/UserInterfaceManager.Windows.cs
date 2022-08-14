using System;
using System.Collections.Generic;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    private readonly Dictionary<Type, Queue<BaseWindow>> _windowsByType = new();
    private readonly Dictionary<Type, Queue<Popup>> _popupsByType = new();

    public T CreatePopup<T>() where T : Popup, new()
    {
        var newPopup = _typeFactory.CreateInstance<T>();
        _popupsByType.GetOrNew(typeof(T)).Enqueue(newPopup);
        ModalRoot.AddChild(newPopup);
        return newPopup;
    }

    public bool RemoveFirstPopup<T>() where T : Popup, new()
    {
        if (!_popupsByType.TryGetValue(typeof(T),out var popupQueue)) return false;
        var oldPopup = popupQueue.Dequeue();
        if (popupQueue.Count == 0)
        {
            _popupsByType.Remove(typeof(T));
        }
        oldPopup.Close();
        oldPopup.Dispose();
        return true;
    }

    public bool TryGetFirstPopup<T>(out T? popup) where T : Popup, new()
    {
        popup = null;
        var success =  _popupsByType.TryGetValue(typeof(T), out var win);
        if (win is {Count: > 0})
        {
            popup = (T)win.Peek();
        }
        return success;
    }

    public bool TryGetFirstPopup(Type type, out Popup? popup)
    {
        popup = null;
        if (!typeof(Popup).IsAssignableFrom(type)) return false;
        if (!_popupsByType.TryGetValue(type, out var popupQueue) || popupQueue.Count == 0) return false;
        popup = popupQueue.Peek();
        return true;
    }

    public bool RemoveFirstWindow<T>() where T : BaseWindow, new()
    {
        if (!_windowsByType.TryGetValue(typeof(T),out var windowQueue)) return false;
        var oldWindow = windowQueue.Dequeue();
        if (windowQueue.Count == 0)
        {
            _windowsByType.Remove(typeof(T));
        }
        _uiManager.StateRoot.RemoveChild(oldWindow);
        oldWindow.Dispose();
        return true;
    }

    public T GetFirstWindow<T>() where T : BaseWindow, new()
    {
        if (!_windowsByType.TryGetValue(typeof(T), out var windowQueue) || windowQueue.Count == 0)
            throw new Exception("Window of type" + typeof(T) + " not found!");
        return (T)windowQueue.Peek();
    }

    public T CreateWindow<T>() where T : BaseWindow, new()
    {
        //If we sandbox this we break creating engine windows. The argument is type bounded anyway so it only accepts
        //public classes that inherit from BaseWindow.
        var newWindow = _typeFactory.CreateInstanceUnchecked<T>();
        _windowsByType.GetOrNew(typeof(T)).Enqueue(newWindow);
        return newWindow;
    }

    private void RegisterWindowOfType(BaseWindow window)
    {
        if (_windowsByType.ContainsKey(window.GetType())) return;
        _windowsByType.GetOrNew(window.GetType()).Enqueue(window);
    }

    public bool TryGetFirstWindow<T>(out T? window) where T : BaseWindow, new()
    {
        window = null;
        var success =  _windowsByType.TryGetValue(typeof(T), out var win);
        if (win is {Count: > 0})
        {
            window = (T)win.Peek();
        }
        return success;
    }

    public bool TryGetFirstWindow(Type type, out BaseWindow? window)
    {
        window = null;
        if (!typeof(BaseWindow).IsAssignableFrom(type)) return false;
        if (!_windowsByType.TryGetValue(type, out var winQueue) || winQueue.Count == 0) return false;
        window = winQueue.Peek();
        return true;
    }
    public void ClearWindows()
    {
        foreach (var data in _windowsByType)
        {
            data.Value.Dequeue().Dispose();
        }
        _windowsByType.Clear();
    }
}
