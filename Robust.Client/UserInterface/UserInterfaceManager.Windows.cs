using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    private readonly Dictionary<string, BaseWindow> _namedWindows = new();
    private readonly Dictionary<Type, Queue<BaseWindow>> _windowsByType = new();
    private readonly Dictionary<string, (IClydeWindow, WindowRoot)> _popoutWindows = new ();
    private readonly Dictionary<string, Popup> _namedPopups = new();
    private readonly Dictionary<Type, Queue<Popup>> _popupsByType = new();

    public bool TryGetNamedPopup(string popupName, out Popup? popup)
    {
        return _namedPopups.TryGetValue(popupName,out popup);
    }

    public Popup GetNamedPopup(string popupName)
    {
        return _namedPopups[popupName];
    }

    public bool RemoveNamedPopup(string popupName)
    {
        if(!_namedPopups.TryGetValue(popupName, out var popup)) return false;
        popup.Close();
        _namedPopups.Remove(popupName);
        popup.Dispose();
        return true;
    }

    public bool RegisterNamedPopup(string popupName, Popup popup)
    {
        return _namedPopups.TryAdd(popupName, popup);
    }

    public T CreatePopupOfType<T>() where T : Popup, new()
    {
        var newPopup = _typeFactory.CreateInstance<T>();
        _popupsByType.GetOrNew(typeof(T)).Enqueue(newPopup);
        ModalRoot.AddChild(newPopup);
        return newPopup;
    }

    public T? CreateNamedPopup<T>(string popupName, Vector2 position) where T : Popup, new()
    {
        if (_namedPopups.ContainsKey(popupName)) return null;
        var popup = CreatePopupOfType<T>();
        popup.Name = popupName;
        popup.Position = position;
        _namedPopups.Add(popupName, popup);
        return popup;
    }

    public bool RemoveFirstPopupOfType<T>() where T : Popup, new()
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
    public bool TryGetFirstPopupByType<T>(out T? popup) where T : Popup, new()
    {
        popup = null;
        var success =  _popupsByType.TryGetValue(typeof(T), out var win);
        if (win is {Count: > 0})
        {
            popup = (T)win.Peek();
        }
        return success;
    }

    public bool TryGetFirstPopupByType(Type type, out Popup? popup)
    {
        popup = null;
        if (!typeof(Popup).IsAssignableFrom(type)) return false;
        if (!_popupsByType.TryGetValue(type, out var popupQueue) || popupQueue.Count == 0) return false;
        popup = popupQueue.Peek();
        return true;
    }

    public bool TryGetPopupWindow(string windowName, out WindowRoot? window)
    {
        window = null;
        if (!_popoutWindows.TryGetValue(windowName, out var data)) return false;
        window = data.Item2;
        return true;
    }

    public bool RemovePopupWindow(string windowName)
    {
        if (!_popoutWindows.TryGetValue(windowName, out var data)) return false;
        data.Item2.Dispose();
        data.Item1.Dispose();
        _popoutWindows.Remove(windowName);
        return true;
    }

    public WindowRoot GetPopupWindow(string windowName)
    {
        if (!_popoutWindows.TryGetValue(windowName, out var data))
            throw new Exception("Could not find popup window of name: " + windowName);
        return data.Item2;
    }

    public WindowRoot? CreatePopupWindow(string windowName,string? displayName = null, int width = 1000, int height = 1000, Action<WindowRequestClosedEventArgs>? OnClosed = null)
    {
        var disName = displayName ?? windowName;
        var monitor = _clyde.EnumerateMonitors().First();
        if (_popoutWindows.ContainsKey(windowName)) return null;
        var newWindow = _clyde.CreateWindow(new WindowCreateParameters
            {
                Maximized = false,
                Title = disName,
                Monitor = monitor,
                Width = width,
                Height = height
            }
        );
        if (OnClosed != null)
        {
            newWindow.RequestClosed += OnClosed;
        }
        newWindow.DisposeOnClose = true;
        var root = _uiManager.CreateWindowRoot(newWindow);
        _popoutWindows.Add(windowName, (newWindow,root));
        return root;
    }

    public bool RegisterNamedWindow(string name, BaseWindow window)
    {
        if (_namedWindows.ContainsKey(name)) return false;
        if (!_windowsByType.ContainsKey(window.GetType()))
        {
            RegisterWindowOfType(window);
        }
        _namedWindows[name] = window;
        _uiManager.StateRoot.AddChild(window);
        return true;
    }

    public BaseWindow GetNamedWindow(string name)
    {
        return _namedWindows[name];
    }

    public bool TryGetNamedWindow(string name, out BaseWindow? window)
    {
        return _namedWindows.TryGetValue(name, out window);
    }

    public T? CreateNamedWindow<T>(string name) where T : BaseWindow, new()
    {
        if (_namedWindows.ContainsKey(name)) return null;
        var newWindow = CreateWindowOfType<T>();
        newWindow.Name = name;
        _uiManager.StateRoot.AddChild(newWindow);

        if (_windowsByType.TryGetValue(typeof(T), out var queue))
        {
            queue.Enqueue(newWindow!);
        }
        else
        {
            queue = new Queue<BaseWindow>();
            queue.Enqueue(newWindow!);
            _windowsByType.Add(typeof(T),queue);
        }
        return newWindow;
    }

    public bool RemoveFirstWindowOfType<T>() where T : BaseWindow, new()
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
    public bool RemoveNamedWindow(string name)
    {
        if (!_namedWindows.TryGetValue(name, out var foundWindow)) return false;
        var windowType = foundWindow.GetType();
        if (_windowsByType.TryGetValue(windowType, out var foundSingleWindow))
        {
            foundSingleWindow.Dequeue();
            if (foundSingleWindow.Count == 0)
            {
                _windowsByType.Remove(windowType);
            }
        }
        _namedWindows.Remove(name);
        _uiManager.StateRoot.RemoveChild(foundWindow);
        foundWindow.Dispose();
        return true;
    }

    public T GetFirstWindowOfType<T>() where T : BaseWindow, new()
    {
        if (!_windowsByType.TryGetValue(typeof(T), out var windowQueue) || windowQueue.Count == 0)
            throw new Exception("Window of type" + typeof(T) + " not found!");
        return (T)windowQueue.Peek();
    }

    public T CreateWindowOfType<T>() where T : BaseWindow, new()
    {
        var newWindow = _typeFactory.CreateInstance<T>();
        _windowsByType.GetOrNew(typeof(T)).Enqueue(newWindow);
        return newWindow;
    }

    private void RegisterWindowOfType(BaseWindow window)
    {
        if (_windowsByType.ContainsKey(window.GetType())) return;
        _windowsByType.GetOrNew(window.GetType()).Enqueue(window);
    }

    public bool TryGetFirstWindowByType<T>(out T? window) where T : BaseWindow, new()
    {
        window = null;
        var success =  _windowsByType.TryGetValue(typeof(T), out var win);
        if (win is {Count: > 0})
        {
            window = (T)win.Peek();
        }
        return success;
    }

    public bool TryGetFirstWindowByType(Type type, out BaseWindow? window)
    {
        window = null;
        if (!typeof(BaseWindow).IsAssignableFrom(type)) return false;
        if (!_windowsByType.TryGetValue(type, out var winQueue) || winQueue.Count == 0) return false;
        window = winQueue.Peek();
        return true;
    }

    private void OnStateUpdated(StateChangedEventArgs args)
    {
        CleanupWindowData();
    }

    private void CleanupWindowData()
    {
        foreach (var data in _windowsByType)
        {
            data.Value.Dequeue().Dispose();
        }
        _windowsByType.Clear();
        _namedWindows.Clear();
        foreach (var (_, value) in _popoutWindows)
        {
            value.Item2.Dispose();
            value.Item1.Dispose();
        }
        _popoutWindows.Clear();
    }
}
