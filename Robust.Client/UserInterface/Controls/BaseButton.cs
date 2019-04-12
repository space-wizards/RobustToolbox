using System;
using JetBrains.Annotations;
using Robust.Client.GodotGlue;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [PublicAPI]
    [ControlWrap(typeof(Godot.BaseButton))]
    public abstract class BaseButton : Control
    {
        private bool _attemptingPress;
        private bool _beingHovered;

        public BaseButton()
        {
        }

        public BaseButton(string name) : base(name)
        {
        }

        internal BaseButton(Godot.BaseButton button) : base(button)
        {
        }

        private ActionMode _mode = ActionMode.Release;

        [ViewVariables]
        public ActionMode Mode
        {
            get => GameController.OnGodot ? (ActionMode) SceneControl.Get("action_mode") : _mode;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("action_mode", (Godot.BaseButton.ActionModeEnum) value);
                }
                else
                {
                    _mode = value;
                }
            }
        }

        private bool _disabled;

        [ViewVariables]
        public bool Disabled
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("disabled") : _disabled;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("disabled", value);
                }
                else
                {
                    var old = _disabled;
                    _disabled = value;

                    if (old != value)
                    {
                        DrawModeChanged();
                    }
                }
            }
        }

        private bool _pressed;

        /// <summary>
        ///     Whether the button is currently toggled down. Only applies when <see cref="ToggleMode"/> is true.
        /// </summary>
        [ViewVariables]
        public bool Pressed
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("pressed") : _pressed;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("pressed", value);
                }
                else
                {
                    if (_pressed != value)
                    {
                        _pressed = value;

                        DrawModeChanged();
                    }
                }
            }
        }

        private bool _toggleMode;

        [ViewVariables]
        public bool ToggleMode
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("toggle_mode") : _toggleMode;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("toggle_mode", value);
                }
                else
                {
                    _toggleMode = value;
                }
            }
        }

        public enum ActionMode
        {
            Press = 0,
            Release = 1,
        }

        [ViewVariables]
        public bool IsHovered => GameController.OnGodot ? (bool) SceneControl.Call("is_hovered") : _beingHovered;

        [ViewVariables]
        public DrawModeEnum DrawMode
        {
            get
            {
                if (GameController.OnGodot)
                {
                    return (DrawModeEnum) SceneControl.Call("get_draw_mode");
                }

                if (Disabled)
                {
                    return DrawModeEnum.Disabled;
                }

                if (Pressed || _attemptingPress)
                {
                    return DrawModeEnum.Pressed;
                }

                if (IsHovered)
                {
                    return DrawModeEnum.Hover;
                }

                return DrawModeEnum.Normal;
            }
        }

        private ButtonGroup _buttonGroup;

        public ButtonGroup ButtonGroup
        {
            get => _buttonGroup ?? (GameController.OnGodot
                       ? new ButtonGroup((Godot.ButtonGroup) SceneControl.Call("get_button_group"))
                       : null);
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Call("set_button_group", value?.GodotGroup);
                }
            }
        }

        public event Action<ButtonEventArgs> OnButtonDown;
        public event Action<ButtonEventArgs> OnButtonUp;
        public event Action<ButtonEventArgs> OnPressed;
        public event Action<ButtonToggledEventArgs> OnToggled;

        protected virtual void DrawModeChanged()
        {
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            if (GameController.OnGodot || Disabled)
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this);
            OnButtonDown?.Invoke(buttonEventArgs);

            var drawMode = DrawMode;
            if (Mode == ActionMode.Release)
            {
                _attemptingPress = true;
            }
            else
            {
                if (ToggleMode)
                {
                    _pressed = !_pressed;
                    OnPressed?.Invoke(buttonEventArgs);
                    OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this));
                }
                else
                {
                    _attemptingPress = true;
                    OnPressed?.Invoke(buttonEventArgs);
                }
            }

            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        protected internal override void MouseUp(GUIMouseButtonEventArgs args)
        {
            base.MouseUp(args);

            if (GameController.OnGodot || Disabled)
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this);
            OnButtonUp?.Invoke(buttonEventArgs);

            var drawMode = DrawMode;
            if (Mode == ActionMode.Release && _attemptingPress)
            {
                if (ToggleMode)
                {
                    _pressed = !_pressed;
                }

                OnPressed?.Invoke(buttonEventArgs);
                if (ToggleMode)
                {
                    OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this));
                }
            }

            _attemptingPress = false;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        protected internal override void MouseEntered()
        {
            base.MouseEntered();

            if (GameController.OnGodot)
            {
                return;
            }

            var drawMode = DrawMode;
            _beingHovered = true;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        protected internal override void MouseExited()
        {
            base.MouseExited();

            if (GameController.OnGodot)
            {
                return;
            }

            var drawMode = DrawMode;
            _beingHovered = false;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

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
            OnToggled?.Invoke(new ButtonToggledEventArgs((bool) state, this));
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            switch (property)
            {
                case "toggle_mode":
                    ToggleMode = (bool) value;
                    break;
                case "disabled":
                    Disabled = (bool) value;
                    break;
            }
        }
    }

    public sealed class ButtonGroup
    {
        public Godot.ButtonGroup GodotGroup { get; }

        public ButtonGroup()
        {
            if (GameController.OnGodot)
            {
                GodotGroup = new Godot.ButtonGroup();
            }
        }

        public ButtonGroup(Godot.ButtonGroup group)
        {
            GodotGroup = group;
        }

        private bool Equals(ButtonGroup other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GameController.OnGodot)
            {
                return Equals(GodotGroup, other.GodotGroup);
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ButtonGroup grp && Equals(grp);
        }

        public override int GetHashCode()
        {
            return GodotGroup != null ? GodotGroup.GetHashCode() : 0;
        }

        public static bool operator ==(ButtonGroup left, ButtonGroup right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ButtonGroup left, ButtonGroup right)
        {
            return !Equals(left, right);
        }
    }
}
