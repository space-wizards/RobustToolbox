using SS14.Client.Input;
using SS14.Client.UserInterface;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Utility;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        UITheme Theme { get; }

        Control Focused { get; }

        Control StateRoot { get; }

        Control WindowRoot { get; }

        /// <summary>
        ///     The "root" control to which all other controls are parented,
        ///     potentially indirectly.
        /// </summary>
        Control RootControl { get; }

        IDebugMonitors DebugMonitors { get; }

        void Popup(string contents, string title = "Alert!");

        Control MouseGetControl(Vector2 coordinates);
    }

    internal interface IUserInterfaceManagerInternal : IUserInterfaceManager
    {
        /// <summary>
        ///     Clears and disposes of all UI components.
        ///     Highly destructive!
        /// </summary>
        void DisposeAllComponents();

        void Initialize();

        void Update(ProcessFrameEventArgs args);

        void FrameUpdate(RenderFrameEventArgs args);

        void MouseDown(MouseButtonEventArgs args);

        void MouseUp(MouseButtonEventArgs args);

        void GDUnhandledMouseDown(MouseButtonEventArgs args);

        void GDUnhandledMouseUp(MouseButtonEventArgs args);

        void GDFocusEntered(Control control);

        void GDFocusExited(Control control);

        void GDPreKeyDown(KeyEventArgs args);

        void GDPreKeyUp(KeyEventArgs args);

        void Render(IRenderHandle renderHandle);

        Dictionary<(GodotAsset asset, int resourceId), object> GodotResourceInstanceCache { get; }
    }
}

