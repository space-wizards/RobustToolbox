using System;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface;

public static class BoundUserInterfaceExt
{
    /// <summary>
    /// Internal method to create a window with default constructor and register it with the BUI system.
    /// </summary>
    private static T GetWindow<T>(BoundUserInterface bui, UserInterfaceSystem system) where T : BaseWindow, new()
    {
        var window = bui.CreateDisposableControl<T>();

        window.OnClose += bui.Close;
        system.RegisterControl(bui, window);

        return window;
    }

    /// <summary>
    /// Internal method to create a window with constructor parameters and register it with the BUI system.
    /// </summary>
    private static T GetWindow<T>(BoundUserInterface bui, object[] args, UserInterfaceSystem system) where T : BaseWindow, IDisposable
    {
        var window = (T?)Activator.CreateInstance(typeof(T), args)
            ?? throw new InvalidOperationException($"Failed to create window of type {typeof(T)}");

        window.OnClose += bui.Close;
        system.RegisterControl(bui, window);

        bui.Disposals ??= [];
        bui.Disposals.Add(window);

        return window;
    }

    /// <summary>
    /// Helper method to create a window and also handle closing the BUI when it's closed.
    /// </summary>
    public static T CreateWindow<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCentered();
        }

        return window;
    }

    /// <summary>
    /// Helper method to create a window with constructor parameters and also handle closing the BUI when it's closed.
    /// </summary>
    /// <param name="args">Arguments to pass to the window constructor.</param>
    public static T CreateWindow<T>(this BoundUserInterface bui, params object[] args) where T : BaseWindow, IDisposable
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, args, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCentered();
        }

        return window;
    }

    /// <summary>
    /// Creates a window centered to the left.
    /// </summary>
    public static T CreateWindowCenteredLeft<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCenteredLeft();
        }

        return window;
    }

    /// <summary>
    /// Creates a window centered to the left with constructor parameters.
    /// </summary>
    /// <param name="args">Arguments to pass to the window constructor.</param>
    public static T CreateWindowCenteredLeft<T>(this BoundUserInterface bui, params object[] args) where T : BaseWindow, IDisposable
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, args, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCenteredLeft();
        }

        return window;
    }

    /// <summary>
    /// Creates a window centered to the right.
    /// </summary>
    public static T CreateWindowCenteredRight<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCenteredRight();
        }

        return window;
    }

    /// <summary>
    /// Creates a window centered to the right with constructor parameters.
    /// </summary>
    /// <param name="args">Arguments to pass to the window constructor.</param>
    public static T CreateWindowCenteredRight<T>(this BoundUserInterface bui, params object[] args) where T : BaseWindow, IDisposable
    {
        var system = bui.EntMan.System<UserInterfaceSystem>();
        var window = GetWindow<T>(bui, args, system);

        if (system.TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCenteredRight();
        }

        return window;
    }

    /// <summary>
    /// Creates a control for this BUI that will be disposed when it is disposed.
    /// </summary>
    public static T CreateDisposableControl<T>(this BoundUserInterface bui) where T : Control, IDisposable, new()
    {
        var control = new T();

        bui.Disposals ??= [];
        bui.Disposals.Add(control);

        return control;
    }

    /// <summary>
    /// Creates a control for this BUI with constructor parameters that will be disposed when it is disposed.
    /// </summary>
    /// <param name="args">Arguments to pass to the control constructor.</param>
    public static T CreateDisposableControl<T>(this BoundUserInterface bui, params object[] args) where T : Control, IDisposable
    {
        var control = (T?)Activator.CreateInstance(typeof(T), args)
            ?? throw new InvalidOperationException($"Failed to create control of type {typeof(T)}");

        bui.Disposals ??= [];
        bui.Disposals.Add(control);

        return control;
    }
}
