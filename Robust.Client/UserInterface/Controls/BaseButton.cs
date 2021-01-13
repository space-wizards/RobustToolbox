using System;
using System.Collections.Generic;
using Robust.Shared.Input;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Base class for a generic UI button.
    /// </summary>
    /// <seealso cref="Button"/>
    /// <seealso cref="TextureButton"/>
    /// <seealso cref="CheckBox"/>
    public abstract class BaseButton : Control
    {
        private bool _attemptingPress;
        private bool _beingHovered;
        private bool _disabled;
        private bool _pressed;
        private bool _enableAllKeybinds;
        private ButtonGroup? _group;
        private bool _toggleMode;

        /// <summary>
        ///     Specifies the group this button belongs to.
        /// </summary>
        /// <remarks>
        ///     Of multiple buttons in the same group, only one can be pressed (radio buttons).
        /// </remarks>
        public ButtonGroup? Group
        {
            get => _group;
            set
            {
                // Remove from old group.
                _group?.Buttons.Remove(this);

                _group = value;

                if (value == null)
                {
                    return;
                }

                value.Buttons.Add(this);
                ToggleMode = true;

                // Set us to pressed if we're the first button.
                Pressed = value.Buttons.Count == 0;
            }
        }

        /// <summary>
        ///     Controls mode of operation in relation to press/release events.
        /// </summary>
        [ViewVariables]
        public ActionMode Mode { get; set; } = ActionMode.Release;

        /// <summary>
        ///     Whether the button is disabled.
        ///     If a button is disabled, it appears greyed out and cannot be interacted with.
        /// </summary>
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

                if (!value && Group != null)
                {
                    throw new InvalidOperationException("Cannot directly unset a grouped button. Set another button in the group instead.");
                }

                _pressed = value;

                if (Group != null)
                {
                    UnsetOtherGroupButtons();
                }

                DrawModeChanged();
            }
        }

        /// <summary>
        ///     Whether key functions other than <see cref="EngineKeyFunctions.UIClick"/> trigger the button.
        /// </summary>
        public bool EnableAllKeybinds
        {
            get => _enableAllKeybinds;
            set => _enableAllKeybinds = value;
        }

        /// <summary>
        ///     If <c>true</c>, this button functions as a toggle, not as a regular push button.
        /// </summary>
        [ViewVariables]
        public bool ToggleMode
        {
            get => _toggleMode;
            set
            {
                if (Group != null && !value)
                {
                    throw new InvalidOperationException("Cannot disable toggle mode on a button in a group.");
                }

                _toggleMode = value;
            }
        }

        /// <summary>
        ///     If <c>true</c>, this button is currently being hovered over by the mouse.
        /// </summary>
        [ViewVariables]
        public bool IsHovered => _beingHovered;

        /// <summary>
        ///     Draw mode used for styling of buttons.
        /// </summary>
        [ViewVariables]
        public DrawModeEnum DrawMode
        {
            get
            {
                if (Disabled)
                {
                    return DrawModeEnum.Disabled;
                }
                else if (Pressed || _attemptingPress)
                {
                    return DrawModeEnum.Pressed;
                }
                else if (IsHovered)
                {
                    return DrawModeEnum.Hover;
                }
                else
                {
                    return DrawModeEnum.Normal;
                }
            }
        }

        /// <summary>
        ///     Fired when the button is pushed down by the mouse.
        /// </summary>
        public event Action<ButtonEventArgs>? OnButtonDown;

        /// <summary>
        ///     Fired when the button is released by the mouse.
        /// </summary>
        public event Action<ButtonEventArgs>? OnButtonUp;

        /// <summary>
        ///     Fired when the button is "pressed". When this happens depends on <see cref="Mode"/>.
        /// </summary>
        public event Action<ButtonEventArgs>? OnPressed;

        /// <summary>
        ///     If <see cref="ToggleMode"/> is set, fired when the button is toggled up or down.
        /// </summary>
        public event Action<ButtonToggledEventArgs>? OnToggled;

        protected BaseButton()
        {
            MouseFilter = MouseFilterMode.Stop;
        }

        protected virtual void DrawModeChanged()
        {
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (Disabled || (!_enableAllKeybinds && args.Function != EngineKeyFunctions.UIClick))
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this, args);
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
                    // Can't un press a radio button directly.
                    if (Group == null || !Pressed)
                    {
                        Pressed = !Pressed;
                        OnPressed?.Invoke(buttonEventArgs);
                        OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this, args));
                        UnsetOtherGroupButtons();
                    }
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

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (Disabled || (!_enableAllKeybinds && args.Function != EngineKeyFunctions.UIClick))
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this, args);
            OnButtonUp?.Invoke(buttonEventArgs);

            var drawMode = DrawMode;
            if (Mode == ActionMode.Release && _attemptingPress && HasPoint((args.PointerLocation.Position - GlobalPixelPosition) / UIScale))
            {
                // Can't un press a radio button directly.
                if (Group == null || !Pressed)
                {
                    if (ToggleMode)
                    {
                        Pressed = !Pressed;
                    }

                    OnPressed?.Invoke(buttonEventArgs);
                    if (ToggleMode && args.CanFocus)
                    {
                        OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this, args));
                        UnsetOtherGroupButtons();
                    }
                }
            }

            _attemptingPress = false;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        private void UnsetOtherGroupButtons()
        {
            if (_group == null)
            {
                return;
            }

            foreach (var button in _group.Buttons)
            {
                if (button != this && button.Pressed)
                {
                    button._pressed = false;
                    button.DrawModeChanged();
                }
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

        public enum DrawModeEnum : byte
        {
            Normal = 0,
            Pressed = 1,
            Hover = 2,
            Disabled = 3
        }

        public class ButtonEventArgs : EventArgs
        {
            /// <summary>
            ///     The button this event originated from.
            /// </summary>
            public BaseButton Button { get; }

            public GUIBoundKeyEventArgs Event { get; }

            public ButtonEventArgs(BaseButton button, GUIBoundKeyEventArgs args)
            {
                Button = button;
                Event = args;
            }
        }

        /// <summary>
        ///     Fired when a <see cref="BaseButton"/> is toggled.
        /// </summary>
        public class ButtonToggledEventArgs : ButtonEventArgs
        {
            /// <summary>
            ///     The new pressed state of the button.
            /// </summary>
            public bool Pressed { get; }

            public ButtonToggledEventArgs(bool pressed, BaseButton button, GUIBoundKeyEventArgs args) : base(button, args)
            {
                Pressed = pressed;
            }
        }

        /// <summary>
        ///     For use with <see cref="BaseButton.Mode"/>.
        /// </summary>
        public enum ActionMode : byte
        {
            /// <summary>
            ///     <see cref="BaseButton.OnPressed"/> fires when the mouse button causing them is pressed down.
            /// </summary>
            Press = 0,

            /// <summary>
            ///     <see cref="BaseButton.OnPressed"/> fires when the mouse button causing them is released.
            ///     This is the default and most intuitive method.
            /// </summary>
            Release = 1
        }
    }

    /// <summary>
    ///     Represents a group of buttons.
    /// </summary>
    /// <remarks>
    ///     Of all buttons in a group, only one can be pressed down.
    ///     Yes, it's for radio buttons.
    /// </remarks>
    public sealed class ButtonGroup
    {
        internal readonly List<BaseButton> Buttons = new();
    }
}
