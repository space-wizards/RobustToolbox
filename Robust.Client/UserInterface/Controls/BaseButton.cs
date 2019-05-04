using System;
using JetBrains.Annotations;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [PublicAPI]
    [ControlWrap("BaseButton")]
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

        [ViewVariables]
        public ActionMode Mode { get; set; } = ActionMode.Release;

        private bool _disabled;

        [ViewVariables]
        public bool Disabled
        {
            get => _disabled;
            set
            {
                var old = _disabled;
                _disabled = value;

                if (old != value)
                {
                    DrawModeChanged();
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
            get => _pressed;
            set
            {
                if (_pressed == value)
                {
                    return;
                }
                _pressed = value;

                DrawModeChanged();
            }
        }

        [ViewVariables]
        public bool ToggleMode { get; set; }

        public enum ActionMode
        {
            Press = 0,
            Release = 1,
        }

        [ViewVariables]
        public bool IsHovered => _beingHovered;

        [ViewVariables]
        public DrawModeEnum DrawMode
        {
            get
            {
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

        public ButtonGroup ButtonGroup { get; set; }

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

            if (Disabled)
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

            if (Disabled)
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
    }
}
