#if GODOT
using SS14.Client.GodotGlue;
#endif
using System;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.BaseButton))]
    #endif
    public abstract class BaseButton : Control
    {
        public BaseButton() : base()
        {
        }
        public BaseButton(string name) : base(name)
        {
        }

        #if GODOT
        internal BaseButton(Godot.BaseButton button) : base(button)
        {
        }

        new private Godot.BaseButton SceneControl;

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.BaseButton)control;
        }
        #endif

        public ActionMode Mode
        {
            #if GODOT
            get => (ActionMode)SceneControl.ActionMode;
            set => SceneControl.ActionMode = (Godot.BaseButton.ActionModeEnum)value;
            #else
            get => default;
            set { }
            #endif
        }

        public bool Disabled
        {
            #if GODOT
            get => SceneControl.Disabled;
            set => SceneControl.Disabled = value;
            #else
            get => default;
            set { }
            #endif
        }

        public bool Pressed
        {
            #if GODOT
            get => SceneControl.Pressed;
            set => SceneControl.Pressed = value;
            #else
            get => default;
            set { }
            #endif
        }

        public bool ToggleMode
        {
            #if GODOT
            get => SceneControl.ToggleMode;
            set => SceneControl.ToggleMode = value;
            #else
            get => default;
            set { }
            #endif
        }

        public enum ActionMode
        {
            Press = 0,
            Release = 1,
        }

        #if GODOT
        public bool IsHovered => SceneControl.IsHovered();
        public DrawModeEnum DrawMode => (DrawModeEnum)SceneControl.GetDrawMode();
        #else
        public bool IsHovered => false;
        public DrawModeEnum DrawMode => DrawModeEnum.Normal;
        #endif

        public ButtonGroup ButtonGroup
        {
            #if GODOT
            get => new ButtonGroup(SceneControl.GetButtonGroup());
            set => SceneControl.SetButtonGroup(value?.GodotGroup);
            #else
            get => default;
            set { }
            #endif
        }

        public event Action<ButtonEventArgs> OnButtonDown;
        public event Action<ButtonEventArgs> OnButtonUp;
        public event Action<ButtonEventArgs> OnPressed;
        public event Action<ButtonToggledEventArgs> OnToggled;

        public enum DrawModeEnum
        {
            Normal = 0,
            Pressed = 1,
            Hover = 2,
            Disabled = 3,
        }

        public class ButtonEventArgs : EventArgs
        {
            /// <summary>
            ///     The button this event originated from.
            /// </summary>
            public BaseButton Button { get; }

            public ButtonEventArgs(BaseButton button)
            {
                Button = button;
            }
        }

        public class ButtonToggledEventArgs : ButtonEventArgs
        {
            /// <summary>
            ///     The new pressed state of the button.
            /// </summary>
            public bool Pressed { get; }

            public ButtonToggledEventArgs(bool pressed, BaseButton button) : base(button)
            {
                Pressed = pressed;
            }
        }

        #if GODOT
        private GodotSignalSubscriber0 __buttonDownSubscriber;
        private GodotSignalSubscriber0 __buttonUpSubscriber;
        private GodotSignalSubscriber1 __toggledSubscriber;
        private GodotSignalSubscriber0 __pressedSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __buttonDownSubscriber = new GodotSignalSubscriber0();
            __buttonDownSubscriber.Connect(SceneControl, "button_down");
            __buttonDownSubscriber.Signal += __buttonDownHook;

            __buttonUpSubscriber = new GodotSignalSubscriber0();
            __buttonUpSubscriber.Connect(SceneControl, "button_up");
            __buttonUpSubscriber.Signal += __buttonUpHook;

            __toggledSubscriber = new GodotSignalSubscriber1();
            __toggledSubscriber.Connect(SceneControl, "toggled");
            __toggledSubscriber.Signal += __toggledHook;

            __pressedSubscriber = new GodotSignalSubscriber0();
            __pressedSubscriber.Connect(SceneControl, "pressed");
            __pressedSubscriber.Signal += __pressedHook;
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (__buttonDownSubscriber != null)
            {
                __buttonDownSubscriber.Disconnect(SceneControl, "button_down");
                __buttonDownSubscriber.Dispose();
                __buttonDownSubscriber = null;
            }

            if (__buttonUpSubscriber != null)
            {
                __buttonUpSubscriber.Disconnect(SceneControl, "button_up");
                __buttonUpSubscriber.Dispose();
                __buttonUpSubscriber = null;
            }

            if (__toggledSubscriber != null)
            {
                __toggledSubscriber.Disconnect(SceneControl, "toggled");
                __toggledSubscriber.Dispose();
                __toggledSubscriber = null;
            }

            if (__pressedSubscriber != null)
            {
                __pressedSubscriber.Disconnect(SceneControl, "pressed");
                __pressedSubscriber.Dispose();
                __pressedSubscriber = null;
            }
        }

        private void __buttonDownHook()
        {
            OnButtonDown?.Invoke(new ButtonEventArgs(this));
        }

        private void __buttonUpHook()
        {
            OnButtonUp?.Invoke(new ButtonEventArgs(this));
        }

        private void __pressedHook()
        {
            OnPressed?.Invoke(new ButtonEventArgs(this));
        }

        private void __toggledHook(object state)
        {
            OnToggled?.Invoke(new ButtonToggledEventArgs((bool)state, this));
        }
        #endif
    }

    public class ButtonGroup
    {
        #if GODOT
        public Godot.ButtonGroup GodotGroup { get; }

        public ButtonGroup() : this(new Godot.ButtonGroup()) { }
        public ButtonGroup(Godot.ButtonGroup group)
        {
            GodotGroup = group;
        }
        #endif
    }
}
