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
        var window = CreateRemovableControl<T>(bui);
        window.OpenCentered();
        window.OnClose += bui.Close;
        return window;
    }

    /// <summary>
    /// Creates a control for this BUI that will be removed from the UI tree when the BUI is disposed.
    /// </summary>
    public static T CreateRemovableControl<T>(this BoundUserInterface bui) where T : Control, new()
    {
        var control = new T();
        AddDisposal(bui, () => control.Orphan());
        return control;
    }

    /// <summary>
    /// Creates a control for this BUI that will be disposed when it is disposed.
    /// </summary>
    /// <param name="bui"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [Obsolete("Control disposal is obsolete and should not be used")]
    public static T CreateDisposableControl<T>(this BoundUserInterface bui) where T : Control, IDisposable, new()
    {
        var control = new T();
        AddDisposal(bui, () => control.Dispose());
        return control;
    }

    private static void AddDisposal(this BoundUserInterface bui, Action act)
    {
        bui.Disposals ??= [];
        bui.Disposals.Add(act);
    }
}
