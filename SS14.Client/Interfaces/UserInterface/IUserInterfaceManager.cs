using SS14.Client.Input;
using SS14.Client.UserInterface;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        /// <summary>
        ///     Clears and disposes of all UI components.
        ///     Highly destructive!
        /// </summary>
        void DisposeAllComponents();

        Control Focused { get; }

        Control StateRoot { get; }

        Control WindowRoot { get; }

        /// <summary>
        ///     The "root" control to which all other controls are parented,
        ///     potentially indirectly.
        /// </summary>
        Control RootControl { get; }

        bool ShowFPS { get; set; }
        bool ShowCoordDebug { get; set; }

        void Initialize();

        void Update(FrameEventArgs args);

        void Popup(string contents, string title="Alert!");

        void UnhandledKeyDown(KeyEventArgs args);

        void UnhandledKeyUp(KeyEventArgs args);

        void UnhandledMouseDown(MouseButtonEventArgs args);

        void UnhandledMouseUp(MouseButtonEventArgs args);

        void FocusEntered(Control control);

        void FocusExited(Control control);

        void PreKeyDown(KeyEventArgs args);

        void PreKeyUp(KeyEventArgs args);
    }
}
