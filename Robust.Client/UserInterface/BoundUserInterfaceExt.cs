using System;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface;

public static class BoundUserInterfaceExt
{
    private static T GetWindow<T>(BoundUserInterface bui) where T : BaseWindow, new()
    {
        var window = bui.CreateDisposableControl<T>();
        window.OnClose += bui.Close;
        var system = bui.EntMan.System<UserInterfaceSystem>();
        system.RegisterControl(bui, window);
        return window;
    }

    /// <summary>
    /// Helper method to create a window and also handle closing the BUI when it's closed.
    /// </summary>
    public static T CreateWindow<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var window = GetWindow<T>(bui);

        if (bui.EntMan.System<UserInterfaceSystem>().TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCentered();
        }

        return window;
    }

    public static T CreateWindowCenteredLeft<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var window = GetWindow<T>(bui);

        if (bui.EntMan.System<UserInterfaceSystem>().TryGetPosition(bui.Owner, bui.UiKey, out var position))
        {
            window.Open(position);
        }
        else
        {
            window.OpenCenteredLeft();
        }

        return window;
    }

    public static T CreateWindowCenteredRight<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var window = GetWindow<T>(bui);

        if (bui.EntMan.System<UserInterfaceSystem>().TryGetPosition(bui.Owner, bui.UiKey, out var position))
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
    /// <param name="bui"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T CreateDisposableControl<T>(this BoundUserInterface bui) where T : Control, IDisposable, new()
    {
        var control = new T();
        bui.Disposals ??= [];
        bui.Disposals.Add(control);
        return control;
    }
}
