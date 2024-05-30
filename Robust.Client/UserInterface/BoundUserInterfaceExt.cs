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
        var window = new T();
        window.OpenCentered();
        window.OnClose += bui.Close;
        return window;
    }
}
