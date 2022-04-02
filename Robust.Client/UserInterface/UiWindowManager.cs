using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

public interface IUIWindowManager
{
    public bool TryGetPopupWindow(string windowName, out WindowRoot? window);
    public bool RemovePopupWindow(string windowName);
    public WindowRoot GetPopupWindow(string windowName);
    public WindowRoot? CreatePopupWindow(string windowName, string? displayName = null, int width = 1000,
        int height = 1000, Action<WindowRequestClosedEventArgs>? onClosed = null);
    public bool RegisterNamedWindow(string name, DefaultWindow window);
    public T? CreateNamedWindow<T>(string name) where T : DefaultWindow, new();
    public bool RemoveWindowOfType<T>() where T : DefaultWindow, new();
    public bool RemoveNamedWindow(string name);
    public T GetFirstWindowOfType<T>() where T : DefaultWindow, new();
    public bool TryGetWindowByType<T>(out T? window) where T : DefaultWindow, new();
    public bool TryGetWindowByType(Type type, out DefaultWindow? window);
    internal void Initialize();
}

[Virtual]
public class UIWindowManager : IUIWindowManager
{
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    private readonly Dictionary<string, DefaultWindow> _windowData = new();
    private readonly Dictionary<Type, Queue<DefaultWindow>> _windowsByType = new();
    private readonly Dictionary<string, (IClydeWindow, WindowRoot)> _popoutWindows = new ();



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

    public bool RegisterNamedWindow(string name, DefaultWindow window)
    {
        if (_windowData.ContainsKey(name)) return false;
        if (!_windowsByType.ContainsKey(window.GetType()))
        {
            RegisterWindowOfType(window);
        }
        _windowData[name] = window;
        _uiManager.StateRoot.AddChild(window);
        return true;
    }

    public T? CreateNamedWindow<T>(string name) where T : DefaultWindow, new()
    {
        if (_windowData.ContainsKey(name)) return null;
        var newWindow = !_windowsByType.ContainsKey(typeof(T)) ? CreateWindowOfType<T>() : _typeFactory.CreateInstance<T>();
        if (newWindow != null) _uiManager.StateRoot.AddChild(newWindow);
        return newWindow;
    }

    public bool RemoveWindowOfType<T>() where T : DefaultWindow, new()
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
        if (!_windowData.TryGetValue(name, out var foundWindow)) return false;
        var windowType = foundWindow.GetType();
        if (_windowsByType.TryGetValue(windowType, out var foundSingleWindow))
        {
            foundSingleWindow.Dequeue();
            if (foundSingleWindow.Count == 0)
            {
                _windowsByType.Remove(windowType);
            }
        }
        _windowData.Remove(name);
        _uiManager.StateRoot.RemoveChild(foundWindow);
        foundWindow.Dispose();
        return true;
    }

    public T GetFirstWindowOfType<T>() where T : DefaultWindow, new()
    {
        if (!_windowsByType.TryGetValue(typeof(T), out var windowQueue) || windowQueue.Count == 0)
            throw new Exception("Window of type" + typeof(T) + " not found!");
        return (T)windowQueue.Peek();
    }

    public T? CreateWindowOfType<T>() where T : DefaultWindow, new()
    {
        if (_windowsByType.ContainsKey(typeof(T))) return null;
        var newWindow = _typeFactory.CreateInstance<T>();
        _windowsByType.GetOrNew(typeof(T)).Enqueue(newWindow);
        return newWindow;
    }

    private void RegisterWindowOfType(DefaultWindow window)
    {
        if (_windowsByType.ContainsKey(window.GetType())) return;
        _windowsByType.GetOrNew(window.GetType()).Enqueue(window);
    }

    public bool TryGetWindowByType<T>(out T? window) where T : DefaultWindow, new()
    {
        window = null;
        var success =  _windowsByType.TryGetValue(typeof(T), out var win);
        if (win is {Count: > 0})
        {
            window = (T)win.Peek();
        }
        return success;
    }

    public bool TryGetWindowByType(Type type, out DefaultWindow? window)
    {
        window = null;
        if (!typeof(DefaultWindow).IsAssignableFrom(type)) return false;
        if (!_windowsByType.TryGetValue(type, out var winQueue) || winQueue.Count == 0) return false;
        window = winQueue.Peek();
        return true;
    }

    private void OnStateUpdated(StateChangedEventArgs args)
    {
        CleanupData();
    }

    void IUIWindowManager.Initialize()
    {
        _stateManager.OnStateChanged += OnStateUpdated;
    }

    private void CleanupData()
    {
        foreach (var data in _windowsByType)
        {
            data.Value.Dequeue().Dispose();
        }
        _windowsByType.Clear();
        _windowData.Clear();
        foreach (var (_, value) in _popoutWindows)
        {
            value.Item2.Dispose();
            value.Item1.Dispose();
        }
        _popoutWindows.Clear();
    }

    ~UIWindowManager()
    {
        CleanupData();
    }
}
