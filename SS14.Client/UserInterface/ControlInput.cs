using SS14.Client.Input;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.UserInterface
{
    public partial class Control
    {
        protected virtual void MouseEntered()
        {
        }

        protected virtual void MouseExited()
        {
        }

        protected virtual void MouseWheel(GUIMouseWheelEventArgs args)
        {
        }

        protected virtual void MouseDown(GUIMouseButtonEventArgs args)
        {
        }

        protected virtual void MouseUp(GUIMouseButtonEventArgs args)
        {
        }

        public event Action<GUIKeyEventArgs> OnKeyDown;
        protected virtual void KeyDown(GUIKeyEventArgs args)
        {
            OnKeyDown?.Invoke(args);
        }

        protected virtual void KeyUp(GUIKeyEventArgs args)
        {
        }

        protected virtual void KeyHeld(GUIKeyEventArgs args)
        {
        }


        private void HandleGuiInput(Godot.InputEvent input)
        {
            switch (input)
            {
                case Godot.InputEventKey keyEvent:
                    var keyEventArgs = new GUIKeyEventArgs(this,
                                                           (Keyboard.Key)keyEvent.Scancode,
                                                           (UInt32)keyEvent.Unicode,
                                                           keyEvent.Alt,
                                                           keyEvent.Control,
                                                           keyEvent.Shift,
                                                           keyEvent.Command);
                    if (keyEvent.Echo)
                    {
                        KeyHeld(keyEventArgs);
                    }
                    else if (keyEvent.Pressed)
                    {
                        KeyDown(keyEventArgs);
                    }
                    else
                    {
                        KeyUp(keyEventArgs);
                    }
                    break;

                case Godot.InputEventMouseButton buttonEvent:
                    if (buttonEvent.ButtonIndex >= Godot.GD.BUTTON_WHEEL_UP && buttonEvent.ButtonIndex <= Godot.GD.BUTTON_WHEEL_RIGHT)
                    {
                        // Mouse wheel event.
                        var mouseWheelEventArgs = new GUIMouseWheelEventArgs((Mouse.Wheel)buttonEvent.ButtonIndex,
                                                                             this,
                                                                             (Mouse.ButtonMask)buttonEvent.ButtonMask,
                                                                             buttonEvent.GlobalPosition.Convert(),
                                                                             buttonEvent.Position.Convert(),
                                                                             buttonEvent.Alt,
                                                                             buttonEvent.Control,
                                                                             buttonEvent.Shift,
                                                                             buttonEvent.Command);
                        MouseWheel(mouseWheelEventArgs);
                    }
                    else
                    {
                        // Mouse button event.
                        var mouseButtonEventArgs = new GUIMouseButtonEventArgs((Mouse.Button)buttonEvent.ButtonIndex,
                                                                               buttonEvent.Doubleclick,
                                                                               this,
                                                                               (Mouse.ButtonMask)buttonEvent.ButtonMask,
                                                                               buttonEvent.GlobalPosition.Convert(),
                                                                               buttonEvent.Position.Convert(),
                                                                               buttonEvent.Alt,
                                                                               buttonEvent.Control,
                                                                               buttonEvent.Shift,
                                                                               buttonEvent.Command);
                        if (buttonEvent.Pressed)
                        {
                            MouseDown(mouseButtonEventArgs);
                        }
                        else
                        {
                            MouseUp(mouseButtonEventArgs);
                        }
                    }
                    break;
            }
        }
    }

    public class GUIKeyEventArgs : KeyEventArgs
    {
        /// <summary>
        ///     The control spawning this event.
        /// </summary>
        public Control SourceControl { get; }

        /// <summary>
        ///     Mark this event as "handled",
        ///     so it stops propagating to other controls or entities.
        /// </summary>
        public void Handle()
        {
            SourceControl.SceneControl.AcceptEvent();
        }

        public GUIKeyEventArgs(Control sourceControl,
                               Keyboard.Key key,
                               uint unicode,
                               bool alt,
                               bool control,
                               bool shift,
                               bool system)
            : base(key, unicode, alt, control, shift, system)
        {
            SourceControl = sourceControl;
        }
    }

    public abstract class GUIMouseEventArgs : ModifierInputEventArgs
    {
        /// <summary>
        ///     The control spawning this event.
        /// </summary>
        public Control SourceControl { get; }

        /// <summary>
        ///     <c>InputEventMouse.button_mask</c> in Godot.
        ///     Which mouse buttons are currently held maybe?
        /// </summary>
        public Mouse.ButtonMask ButtonMask { get; }

        /// <summary>
        ///     Position of the mouse, relative to the screen.
        /// </summary>
        public Vector2 GlobalPosition { get; }

        /// <summary>
        ///     Position of the mouse, relative to the current control.
        /// </summary>
        public Vector2 RelativePosition { get; }

        /// <summary>
        ///     Mark this event as "handled",
        ///     so it stops propagating to other controls or entities.
        /// </summary>
        public void Handle()
        {
            SourceControl.SceneControl.AcceptEvent();
        }

        protected GUIMouseEventArgs(Control sourceControl,
                                    Mouse.ButtonMask buttonMask,
                                    Vector2 globalPosition,
                                    Vector2 relativePosition,
                                    bool alt,
                                    bool control,
                                    bool shift,
                                    bool system)
            : base(alt, control, shift, system)
        {
            SourceControl = sourceControl;
            ButtonMask = buttonMask;
            GlobalPosition = globalPosition;
            RelativePosition = relativePosition;
        }
    }

    public class GUIMouseButtonEventArgs : GUIMouseEventArgs
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

        public GUIMouseButtonEventArgs(Mouse.Button button,
                                       bool doubleClick,
                                       Control sourceControl,
                                       Mouse.ButtonMask buttonMask,
                                       Vector2 globalPosition,
                                       Vector2 relativePosition,
                                       bool alt,
                                       bool control,
                                       bool shift,
                                       bool system)
            : base(sourceControl, buttonMask, globalPosition, relativePosition, alt, control, shift, system)
        {
            Button = button;
            DoubleClick = doubleClick;
        }
    }

    public class GUIMouseMoveEventArgs : GUIMouseEventArgs
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
        public GUIMouseMoveEventArgs(Vector2 relative,
                                     Vector2 speed,
                                     Control sourceControl,
                                     Mouse.ButtonMask buttonMask,
                                     Vector2 globalPosition,
                                     Vector2 relativePosition,
                                     bool alt,
                                     bool control,
                                     bool shift,
                                     bool system)
            : base(sourceControl, buttonMask, globalPosition, relativePosition, alt, control, shift, system)
        {
            Relative = relative;
            Speed = speed;
        }

    }

    public class GUIMouseWheelEventArgs : GUIMouseEventArgs
    {
        /// <summary>
        ///     The direction the mouse wheel was moved in.
        /// </summary>
        public Mouse.Wheel WheelDirection { get; }

        public GUIMouseWheelEventArgs(Mouse.Wheel wheelDirection,
                                         Control sourceControl,
                                         Mouse.ButtonMask buttonMask,
                                         Shared.Maths.Vector2 globalPosition,
                                         Shared.Maths.Vector2 relativePosition,
                                         bool alt,
                                         bool control,
                                         bool shift,
                                         bool system)
            : base(sourceControl, buttonMask, globalPosition, relativePosition, alt, control, shift, system)
        {
            WheelDirection = wheelDirection;
        }
    }

}
