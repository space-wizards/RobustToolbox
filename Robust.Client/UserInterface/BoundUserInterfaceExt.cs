using System;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface;

public static class BoundUserInterfaceExt
{
    /// <summary>
    /// Helper method to create a window and also handle closing the BUI when it's closed.
    /// </summary>
    public static T CreateWindow<T>(this BoundUserInterface bui) where T : BaseWindow, new()
    {
        var window = bui.CreateDisposableControl<T>();
        window.OpenCentered();
        window.OnClose += bui.Close;
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
