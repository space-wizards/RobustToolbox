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
        UITheme ThemeDefaults { get; }
        Stylesheet Stylesheet { get; set; }

        Control KeyboardFocused { get; }

        Control StateRoot { get; }

        Control WindowRoot { get; }

        Control CurrentlyHovered { get; }

        /// <summary>
        ///     The "root" control to which all other controls are parented,
        ///     potentially indirectly.
        /// </summary>
        Control RootControl { get; }

        IDebugMonitors DebugMonitors { get; }

        void Popup(string contents, string title = "Alert!");

        Control MouseGetControl(Vector2 coordinates);

        /// <summary>
        ///     Give a control keyboard focus, releasing focus on the currently focused control (if any).
        /// </summary>
        /// <param name="control">The control to give keyboard focus to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="control"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="control"/> has <see cref="Control.CanKeyboardFocus"/> <c>false</c>.
        /// </exception>
        void GrabKeyboardFocus(Control control);

        /// <summary>
        ///     Release keyboard focus from the currently focused control, if any.
        /// </summary>
        void ReleaseKeyboardFocus();

        /// <summary>
        ///     Conditionally release keyboard focus if <see cref="ifControl"/> has keyboard focus.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="ifControl"/> is <c>null</c>.
        /// </exception>
        /// <seealso cref="ReleaseKeyboardFocus()"/>
        void ReleaseKeyboardFocus(Control ifControl);
    }

    internal interface IUserInterfaceManagerInternal : IUserInterfaceManager
    {
        /// <summary>
        ///     Clears and disposes of all UI components.
        ///     Highly destructive!
        /// </summary>
        void DisposeAllComponents();

        void Initialize();
        void InitializeTesting();

        void Update(ProcessFrameEventArgs args);

        void FrameUpdate(RenderFrameEventArgs args);

        void MouseDown(MouseButtonEventArgs args);

        void MouseUp(MouseButtonEventArgs args);

        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);

        void TextEntered(TextEventArgs textEvent);

        void GDUnhandledMouseDown(MouseButtonEventArgs args);

        void GDUnhandledMouseUp(MouseButtonEventArgs args);

        void GDFocusEntered(Control control);

        void GDFocusExited(Control control);

        void GDMouseEntered(Control control);

        void GDMouseExited(Control control);

        void GDPreKeyDown(KeyEventArgs args);

        void GDPreKeyUp(KeyEventArgs args);

        void Render(IRenderHandle renderHandle);

        Dictionary<(GodotAsset asset, int resourceId), object> GodotResourceInstanceCache { get; }
    }
}

