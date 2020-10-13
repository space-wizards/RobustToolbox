using System;
using Robust.Shared.Map;

namespace Robust.Shared.Input
{
    /// <summary>
    ///     Event data values for a bound key state change.
    /// </summary>
    public class BoundKeyEventArgs : EventArgs
    {
        /// <summary>
        ///     Bound key that that is changing.
        /// </summary>
        public BoundKeyFunction Function { get; }

        /// <summary>
        ///     New state of the <see cref="BoundKeyFunction"/>.
        /// </summary>
        public BoundKeyState State { get; }

        /// <summary>
        ///     Current Pointer location in screen coordinates.
        /// </summary>
        public ScreenCoordinates PointerLocation { get; }

        /// <summary>
        ///     Whether the Bound key can change the focused control.
        /// </summary>
        public bool CanFocus { get; internal set; }

        /// <summary>
        ///     Whether the Bound key is a printable character.
        ///     Used for checking whether hotkey should be blocked so that hotkeys don't get triggered when using UserInterfaceManager.TextEntered.
        /// </summary>
        public bool Printable { get; internal set; }

        public bool Handled { get; private set; }

        /// <summary>
        ///     Constructs a new instance of <see cref="BoundKeyEventArgs"/>.
        /// </summary>
        /// <param name="function">Bound key that that is changing.</param>
        /// <param name="state">New state of the function.</param>
        /// <param name="pointerLocation">Current Pointer location in screen coordinates.</param>
        public BoundKeyEventArgs(BoundKeyFunction function, BoundKeyState state, ScreenCoordinates pointerLocation, bool canFocus, bool printable)
        {
            Function = function;
            State = state;
            PointerLocation = pointerLocation;
            CanFocus = canFocus;
            Printable = printable;
        }

        /// <summary>
        ///     Mark this event as handled.
        /// </summary>
        public void Handle()
        {
            Handled = true;
        }
    }
}
