using System;
using SS14.Client.Interfaces;
using SS14.Client.Utility;
using SS14.Shared.Input;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Input
{
    /// <summary>
    ///     Generic input event that has modifier keys like control.
    /// </summary>
    public abstract class ModifierInputEventArgs : EventArgs
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

        public bool Handled { get; private set; }

        protected ModifierInputEventArgs(bool alt, bool control, bool shift, bool system)
        {
            Alt = alt;
            Control = control;
            Shift = shift;
            System = system;
        }

        /// <summary>
        ///     Mark this event as handled.
        /// </summary>
        public void Handle()
        {
            if (GameController.OnGodot)
            {
                var tree = IoCManager.Resolve<ISceneTreeHolder>();
                tree.SceneTree.SetInputAsHandled();
            }

            Handled = true;
        }
    }

    public class TextEventArgs : EventArgs
    {
        public TextEventArgs(uint codePoint)
        {
            CodePoint = codePoint;
        }

        public uint CodePoint { get; }
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

        public KeyEventArgs(Keyboard.Key key, bool repeat, bool alt, bool control, bool shift, bool system)
            : base(alt, control, shift, system)
        {
            Key = key;
            IsRepeat = repeat;
        }

        public static explicit operator KeyEventArgs(Godot.InputEventKey args)
        {
            return new KeyEventArgs(Keyboard.ConvertGodotKey(args.Scancode),
                args.Echo,
                args.Alt,
                args.Control,
                args.Shift,
                args.Command);
        }

        public static explicit operator KeyEventArgs(Godot.InputEventMouseButton args)
        {
            var key = Mouse.MouseButtonToKey((Mouse.Button) args.ButtonIndex);
            return new KeyEventArgs(key, false, false, false, false, false);
        }

        public static explicit operator KeyEventArgs(OpenTK.Input.KeyboardKeyEventArgs args)
        {
            return new KeyEventArgs(Keyboard.ConvertOpenTKKey(args.Key), args.IsRepeat, args.Alt, args.Control, args.Shift, false);
        }

        public static explicit operator KeyEventArgs(OpenTK.Input.MouseButtonEventArgs args)
        {
            return new KeyEventArgs(Mouse.MouseButtonToKey(Mouse.ConvertOpenTKButton(args.Button)), false,
                false, false, false, false);
        }
    }

    public abstract class MouseEventArgs : ModifierInputEventArgs
    {
        /// <summary>
        ///     <c>InputEventMouse.button_mask</c> in Godot.
        ///     Which mouse buttons are currently held maybe?
        /// </summary>
        public Mouse.ButtonMask ButtonMask { get; }

        /// <summary>
        ///     Position of the mouse relative to the screen.
        /// </summary>
        public Vector2 Position { get; }

        protected MouseEventArgs(Mouse.ButtonMask buttonMask,
            Vector2 position,
            bool alt,
            bool control,
            bool shift,
            bool system)
            : base(alt, control, shift, system)
        {
            ButtonMask = buttonMask;
            Position = position;
        }
    }

    public class MouseButtonEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The mouse button that has been pressed or released.
        /// </summary>
        public Mouse.Button Button { get; }

        /// <summary>
        ///     True if this action was a double click.
        ///     Can't be true if this was a release event.
        /// </summary>
        public bool DoubleClick { get; }

        public ClickType ClickType
        {
            get
            {
                ClickType type = ClickType.None;
                switch (Button)
                {
                    case Mouse.Button.Left:
                        type = ClickType.Left;
                        break;
                    case Mouse.Button.Right:
                        type = ClickType.Right;
                        break;
                    case Mouse.Button.Middle:
                        type = ClickType.Middle;
                        break;
                    default:
                        return type;
                }

                if (Alt)
                    type |= ClickType.Alt;
                if (Control)
                    type |= ClickType.Cntrl;
                if (Shift)
                    type |= ClickType.Shift;
                if (System)
                    type |= ClickType.System;
                return type;
            }
        }

        // ALL the parameters!
        public MouseButtonEventArgs(Mouse.Button button,
            bool doubleClick,
            Mouse.ButtonMask buttonMask,
            Vector2 position,
            bool alt,
            bool control,
            bool shift,
            bool system)
            : base(buttonMask, position, alt, control, shift, system)
        {
            Button = button;
            DoubleClick = doubleClick;
        }

        public static explicit operator MouseButtonEventArgs(Godot.InputEventMouseButton inputEvent)
        {
            // Before cutting this up,
            // this line was 281 characters long.
            return new MouseButtonEventArgs((Mouse.Button) inputEvent.ButtonIndex,
                inputEvent.Doubleclick,
                (Mouse.ButtonMask) inputEvent.ButtonMask,
                inputEvent.Position.Convert(),
                inputEvent.Alt,
                inputEvent.Control,
                inputEvent.Shift,
                inputEvent.Command);
        }

        public static explicit operator MouseButtonEventArgs(OpenTK.Input.MouseButtonEventArgs inputEvent)
        {
            return new MouseButtonEventArgs(
                Mouse.ConvertOpenTKButton(inputEvent.Button),
                false, Mouse.ButtonMask.None,
                new Vector2(inputEvent.X, inputEvent.Y),
                false, false, false, false);
        }
    }

    public class MouseWheelEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The direction the mouse wheel was moved in.
        /// </summary>
        public Mouse.Wheel WheelDirection { get; }

        // ALL the parameters!
        public MouseWheelEventArgs(Mouse.Wheel wheelDirection,
            Mouse.ButtonMask buttonMask,
            Vector2 position,
            bool alt,
            bool control,
            bool shift,
            bool system)
            : base(buttonMask, position, alt, control, shift, system)
        {
            WheelDirection = wheelDirection;
        }

        public static explicit operator MouseWheelEventArgs(Godot.InputEventMouseButton inputEvent)
        {
            // Before cutting this up,
            // this line was 281 characters long.
            return new MouseWheelEventArgs((Mouse.Wheel) inputEvent.ButtonIndex,
                (Mouse.ButtonMask) inputEvent.ButtonMask,
                inputEvent.Position.Convert(),
                inputEvent.Alt,
                inputEvent.Control,
                inputEvent.Shift,
                inputEvent.Command);
        }

        public static explicit operator MouseWheelEventArgs(OpenTK.Input.MouseWheelEventArgs inputEvent)
        {
            var direction = inputEvent.Delta > 0 ? Mouse.Wheel.Up : Mouse.Wheel.Down;
            return new MouseWheelEventArgs(
                direction,
                Mouse.ButtonMask.None,
                new Vector2(inputEvent.X, inputEvent.Y),
                false, false, false, false);
        }
    }

    public class MouseMoveEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     The new position relative to the previous position.
        /// </summary>
        public Vector2 Relative { get; }

        // TODO: Godot's docs aren't exactly clear on what this is.
        //         Speed how?
        /// <summary>
        ///     The speed of the movement.
        /// </summary>
        public Vector2 Speed { get; }

        // ALL the parameters!
        public MouseMoveEventArgs(Vector2 relative,
            Vector2 speed,
            Mouse.ButtonMask buttonMask,
            Vector2 position,
            bool alt,
            bool control,
            bool shift,
            bool system)
            : base(buttonMask, position, alt, control, shift, system)
        {
            Relative = relative;
            Speed = speed;
        }

        public static explicit operator MouseMoveEventArgs(Godot.InputEventMouseMotion inputEvent)
        {
            return new MouseMoveEventArgs(inputEvent.Relative.Convert(),
                inputEvent.Speed.Convert(),
                (Mouse.ButtonMask) inputEvent.ButtonMask,
                inputEvent.Position.Convert(),
                inputEvent.Alt,
                inputEvent.Control,
                inputEvent.Shift,
                inputEvent.Command);
        }

        public static explicit operator MouseMoveEventArgs(OpenTK.Input.MouseMoveEventArgs inputEvent)
        {
            return new MouseMoveEventArgs(
                new Vector2(inputEvent.XDelta, inputEvent.YDelta),
                Vector2.Zero, Mouse.ButtonMask.None,
                new Vector2(inputEvent.X, inputEvent.Y),
                false, false, false, false);
        }
    }
}
