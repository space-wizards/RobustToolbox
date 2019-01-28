using SS14.Client.Input;
using SS14.Client.UserInterface;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using SS14.Client.Graphics.Clyde;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
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

        void UnhandledMouseDown(MouseButtonEventArgs args);

        void UnhandledMouseUp(MouseButtonEventArgs args);

        void FocusEntered(Control control);

        void FocusExited(Control control);

        void PreKeyDown(KeyEventArgs args);

        void PreKeyUp(KeyEventArgs args);

        void Render(IRenderHandle renderHandle);
    }
}
