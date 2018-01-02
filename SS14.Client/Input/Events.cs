using System;
using SS14.Client.Interfaces;
using SS14.Client.Utility;
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
            var tree = IoCManager.Resolve<ISceneTreeHolder>();
            tree.SceneTree.SetInputAsHandled();
        }
    }

    public class KeyEventArgs : ModifierInputEventArgs
    {
        /// <summary>
        ///     The key that got pressed or released.
        /// </summary>
        public Keyboard.Key Key { get; }

        // Going with UInt32 instead of uint to make it clear we need 32 bits!
        // We're not some prehistoric UTF-16 savage.
        /// <summary>
        ///     Unicode code point of the pressed key, if relevant.
        /// </summary>
        public UInt32 Unicode { get; }

        public KeyEventArgs(Keyboard.Key key, UInt32 unicode, bool alt, bool control, bool shift, bool system)
            : base(alt, control, shift, system)
        {
            Key = key;
            Unicode = unicode;
        }

        public static explicit operator KeyEventArgs(Godot.InputEventKey args)
        {
            return new KeyEventArgs((Keyboard.Key)args.Scancode,
                                    (UInt32)args.Unicode,
                                    args.Alt,
                                    args.Control,
                                    args.Shift,
                                    args.Command);
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
            return new MouseButtonEventArgs((Mouse.Button)inputEvent.ButtonIndex,
                                            inputEvent.Doubleclick,
                                            (Mouse.ButtonMask)inputEvent.ButtonMask,
                                            inputEvent.Position.Convert(),
                                            inputEvent.Alt,
                                            inputEvent.Control,
                                            inputEvent.Shift,
                                            inputEvent.Command);
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
            return new MouseWheelEventArgs((Mouse.Wheel)inputEvent.ButtonIndex,
                                            (Mouse.ButtonMask)inputEvent.ButtonMask,
                                            inputEvent.Position.Convert(),
                                            inputEvent.Alt,
                                            inputEvent.Control,
                                            inputEvent.Shift,
                                            inputEvent.Command);
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
                                          (Mouse.ButtonMask)inputEvent.ButtonMask,
                                          inputEvent.Position.Convert(),
                                          inputEvent.Alt,
                                          inputEvent.Control,
                                          inputEvent.Shift,
                                          inputEvent.Command);
        }
    }
}
