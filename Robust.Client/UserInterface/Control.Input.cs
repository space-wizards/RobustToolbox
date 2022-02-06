using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        /// <summary>
        ///     Invoked when the mouse enters the area of this control / when it hovers over the control.
        /// </summary>
        public event Action<GUIMouseHoverEventArgs>? OnMouseEntered;

        protected internal virtual void MouseEntered()
        {
            OnMouseEntered?.Invoke(new GUIMouseHoverEventArgs(this));
        }

        /// <summary>
        ///     Invoked when the mouse exits the area of this control / when it stops hovering over the control.
        /// </summary>
        public event Action<GUIMouseHoverEventArgs>? OnMouseExited;

        protected internal virtual void MouseExited()
        {
            OnMouseExited?.Invoke(new GUIMouseHoverEventArgs(this));
        }

        protected internal virtual void MouseWheel(GUIMouseWheelEventArgs args)
        {
        }

        public event Action<GUIBoundKeyEventArgs>? OnKeyBindDown;
        public event Action<GUIBoundKeyEventArgs>? OnKeyBindUp;

        protected internal virtual void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            OnKeyBindDown?.Invoke(args);
        }

        protected internal virtual void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            OnKeyBindUp?.Invoke(args);
        }

        protected internal virtual void MouseMove(GUIMouseMoveEventArgs args)
        {
        }

        protected internal virtual void KeyHeld(GUIKeyEventArgs args)
        {
        }

        protected internal virtual void TextEntered(GUITextEventArgs args)
        {
        }
    }

    public sealed class GUIMouseHoverEventArgs : EventArgs
    {
        /// <summary>
        ///     The control this event originated from.
        /// </summary>
        public Control SourceControl { get; }

        public GUIMouseHoverEventArgs(Control sourceControl)
        {
            SourceControl = sourceControl;
        }
    }

    public sealed class GUIBoundKeyEventArgs : BoundKeyEventArgs
    {
        /// <summary>
        ///     Position of the mouse, relative to the current control.
        /// </summary>
        public Vector2 RelativePosition { get; internal set; }

        public Vector2 RelativePixelPosition { get; internal set; }

        public GUIBoundKeyEventArgs(BoundKeyFunction function, BoundKeyState state, ScreenCoordinates pointerLocation,
            bool canFocus, Vector2 relativePosition, Vector2 relativePixelPosition)
            : base(function, state, pointerLocation, canFocus)
        {
            RelativePosition = relativePosition;
            RelativePixelPosition = relativePixelPosition;
        }
    }

    public sealed class GUIKeyEventArgs : KeyEventArgs
    {
        /// <summary>
        ///     The control spawning this event.
        /// </summary>
        public Control SourceControl { get; }

        public GUIKeyEventArgs(Control sourceControl,
            Keyboard.Key key,
            bool repeat,
            bool alt,
            bool control,
            bool shift,
            bool system,
            int scanCode)
            : base(key, repeat, alt, control, shift, system, scanCode)
        {
            SourceControl = sourceControl;
        }
    }

    public sealed class GUITextEventArgs : TextEventArgs
    {
        /// <summary>
        ///     The control spawning this event.
        /// </summary>
        public Control SourceControl { get; }

        public GUITextEventArgs(Control sourceControl,
            uint codePoint)
            : base(codePoint)
        {
            SourceControl = sourceControl;
        }
    }

    public abstract class GUIMouseEventArgs : InputEventArgs
    {
        /// <summary>
        ///     The control spawning this event.
        /// </summary>
        public Control SourceControl { get; internal set; }

        /// <summary>
        ///     Position of the mouse, relative to the screen.
        /// </summary>
        public Vector2 GlobalPosition { get; }

        public ScreenCoordinates GlobalPixelPosition { get; }

        /// <summary>
        ///     Position of the mouse, relative to the current control.
        /// </summary>
        public Vector2 RelativePosition { get; internal set; }

        public Vector2 RelativePixelPosition { get; internal set; }

        protected GUIMouseEventArgs(Control sourceControl,
            Vector2 globalPosition,
            ScreenCoordinates globalPixelPosition,
            Vector2 relativePosition,
            Vector2 relativePixelPosition)
        {
            SourceControl = sourceControl;
            GlobalPosition = globalPosition;
            RelativePosition = relativePosition;
            RelativePixelPosition = relativePixelPosition;
            GlobalPixelPosition = globalPixelPosition;
        }
    }

    public sealed class GUIMouseMoveEventArgs : GUIMouseEventArgs
    {
        /// <summary>
        ///     The new position relative to the previous position.
        /// </summary>
        public Vector2 Relative { get; }

        // ALL the parameters!
        public GUIMouseMoveEventArgs(Vector2 relative,
            Control sourceControl,
            Vector2 globalPosition,
            ScreenCoordinates globalPixelPosition,
            Vector2 relativePosition,
            Vector2 relativePixelPosition)
            : base(sourceControl, globalPosition, globalPixelPosition, relativePosition, relativePixelPosition)
        {
            Relative = relative;
        }
    }

    public sealed class GUIMouseWheelEventArgs : GUIMouseEventArgs
    {
        public Vector2 Delta { get; }

        public GUIMouseWheelEventArgs(Vector2 delta,
            Control sourceControl,
            Vector2 globalPosition,
            ScreenCoordinates globalPixelPosition,
            Vector2 relativePosition,
            Vector2 relativePixelPosition)
            : base(sourceControl, globalPosition, globalPixelPosition, relativePosition, relativePixelPosition)
        {
            Delta = delta;
        }
    }
}
