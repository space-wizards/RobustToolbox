using System;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Input
{
    public abstract class InputEventArgs : EventArgs
    {
        public bool Handled { get; private set; }

        /// <summary>
        ///     Mark this event as handled.
        /// </summary>
        public void Handle()
        {
            Handled = true;
        }
    }

    /// <summary>
    ///     Generic input event that has modifier keys like control.
    /// </summary>
    public abstract class ModifierInputEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Whether the alt key (⌥ Option on MacOS) is held.
        /// </summary>
        public bool Alt { get; }

        /// <summary>
        ///     Whether the control key is held.
        /// </summary>
        public bool Control { get; }

        /// <summary>
        ///     Whether the shift key is held.
        /// </summary>
        public bool Shift { get; }

        /// <summary>
        ///     Whether the system key (Windows key, ⌘ Command on MacOS) is held.
        /// </summary>
        public bool System { get; }

        protected ModifierInputEventArgs(bool alt, bool control, bool shift, bool system)
        {
            Alt = alt;
            Control = control;
            Shift = shift;
            System = system;
        }
    }

    public class TextEventArgs : EventArgs
    {
        public TextEventArgs(uint codePoint)
        {
            CodePoint = codePoint;
        }

        public uint CodePoint { get; }
        public Rune AsRune => new Rune(CodePoint);
    }

    public class KeyEventArgs : ModifierInputEventArgs
    {
        /// <summary>
        ///     The key that got pressed or released.
        /// </summary>
        public Keyboard.Key Key { get; }

        /// <summary>
        ///     If true, this key is being held down and another key event is being fired by the OS.
        /// </summary>
        public bool IsRepeat { get; }

        public int ScanCode { get; }

        public KeyEventArgs(
            Keyboard.Key key,
            bool repeat,
            bool alt, bool control, bool shift, bool system,
            int scanCode)
            : base(alt, control, shift, system)
        {
            Key = key;
            IsRepeat = repeat;
            ScanCode = scanCode;
        }
    }

    public abstract class MouseEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Position of the mouse relative to the screen.
        /// </summary>
        public ScreenCoordinates Position { get; }

        protected MouseEventArgs(ScreenCoordinates position)
        {
            Position = position;
        }
    }

    public class MouseButtonEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The mouse button that has been pressed or released.
        /// </summary>
        public Mouse.Button Button { get; }

        // ALL the parameters!
        public MouseButtonEventArgs(Mouse.Button button, ScreenCoordinates position)
            : base(position)
        {
            Button = button;
        }
    }

    public class MouseWheelEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The direction the mouse wheel was moved in.
        /// </summary>
        public Vector2 Delta { get; }

        // ALL the parameters!
        public MouseWheelEventArgs(Vector2 delta, ScreenCoordinates position)
            : base(position)
        {
            Delta = delta;
        }
    }

    public class MouseMoveEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The new position relative to the previous position.
        /// </summary>
        public Vector2 Relative { get; }

        // ALL the parameters!
        public MouseMoveEventArgs(Vector2 relative, ScreenCoordinates position)
            : base(position)
        {
            Relative = relative;
        }
    }

    public class MouseEnterLeaveEventArgs : EventArgs
    {
        public IClydeWindow Window { get; }

        /// <summary>
        ///     True if the mouse ENTERED the window, false if it LEFT the window.
        /// </summary>
        public bool Entered { get; }

        // ALL the parameters!
        public MouseEnterLeaveEventArgs(IClydeWindow window, bool entered)
        {
            Window = window;
            Entered = entered;
        }
    }
}
