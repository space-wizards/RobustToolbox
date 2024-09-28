using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        private int _attemptingPress;
        private bool _beingHovered;
        private bool _disabled;
        private bool _pressed;
        private bool _enableAllKeybinds;
        private ButtonGroup? _group;
        private bool _toggleMode;
        private bool _muteSounds;

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
                if (value?.InternalButtons.Contains(this) ?? false)
                    return; // No work to do.
                // Remove from old group.
                _group?.InternalButtons.Remove(this);

                _group = value;

                if (value == null)
                {
                    return;
                }

                value.InternalButtons.Add(this);
                ToggleMode = true;

                if (value.IsNoneSetAllowed)
                {
                    // Still UNPRESS if there's another pressed button, but don't PRESS it otherwise.
                    if (value.Pressed != this)
                        _pressed = false;
                }
                else
                {
                    // Set us to pressed if we're the first button. Doesn't go through the setter to avoid setting off our own error check.
                    _pressed = value.InternalButtons.Count == 1;
                }
                DrawModeChanged();
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

                if (!value && Group is { IsNoneSetAllowed: false })
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
        /// Sets the button's press state and also handles click sounds.
        /// </summary>
        /// <returns></returns>
        public void SetClickPressed(bool value)
        {
            Pressed = value;

            if (Pressed != value)
                return;

            if (!MuteSounds)
                UserInterfaceManager.ClickSound();
        }

        /// <summary>
        ///     Whether key functions other than <see cref="EngineKeyFunctions.UIClick"/> trigger the button.
        /// </summary>
        [ViewVariables]
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
                else if (Pressed || (_attemptingPress > 0 && IsHovered))
                {
                    return DrawModeEnum.Pressed;
                }
                else if (IsHovered || _attemptingPress > 0)
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
        ///     If <c>true</c>, this button will not emit sounds when the mouse is pressed or hovered over.
        /// </summary>
        [ViewVariables]
        public bool MuteSounds
        {
            get => _muteSounds;
            set => _muteSounds = value;
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

            if (Disabled || args.Function == EngineKeyFunctions.Use || (!_enableAllKeybinds && args.Function != EngineKeyFunctions.UIClick))
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this, args);
            OnButtonDown?.Invoke(buttonEventArgs);

            var drawMode = DrawMode;
            if (Mode == ActionMode.Release)
            {
                UserInterfaceManager.ControlFocused = this;
                _attemptingPress += 1;
            }
            else
            {
                if (ToggleMode)
                {
                    // Can't un press a radio button directly.
                    if (Group == null || !Pressed)
                    {
                        SetClickPressed(!Pressed);
                        OnPressed?.Invoke(buttonEventArgs);
                        OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this, args));
                        UnsetOtherGroupButtons();
                    }
                }
                else
                {
                    UserInterfaceManager.ControlFocused = this;
                    _attemptingPress += 1;
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

            if (Disabled || args.Function == EngineKeyFunctions.Use || (!_enableAllKeybinds && args.Function != EngineKeyFunctions.UIClick))
            {
                return;
            }

            var buttonEventArgs = new ButtonEventArgs(this, args);
            OnButtonUp?.Invoke(buttonEventArgs);

            var drawMode = DrawMode;
            if (Mode == ActionMode.Release && _attemptingPress > 0 && HasPoint((args.PointerLocation.Position - GlobalPixelPosition) / UIScale))
            {
                // Can't un press a radio button directly.
                // Only trigger toggle on UIClick. Do not un-press a toggle button if it's in a group.
                if (args.Function != EngineKeyFunctions.UIClick || Group == null || !Pressed)
                {
                    if (args.Function == EngineKeyFunctions.UIClick && ToggleMode && _attemptingPress == 1)
                    {
                        SetClickPressed(!Pressed);
                    }
                    else
                    {
                        if (!MuteSounds)
                            UserInterfaceManager.ClickSound();
                    }

                    OnPressed?.Invoke(buttonEventArgs);
                    if (args.Function == EngineKeyFunctions.UIClick && ToggleMode)
                    {
                        OnToggled?.Invoke(new ButtonToggledEventArgs(Pressed, this, args));
                        UnsetOtherGroupButtons();
                    }
                }
            }

            if (_attemptingPress > 0)
                _attemptingPress -= 1;
            if (_attemptingPress <= 0 && UserInterfaceManager.ControlFocused == this)
                UserInterfaceManager.ControlFocused = null;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        protected internal override void ControlFocusExited()
        {
            base.ControlFocusExited();

            var drawMode = DrawMode;
            _attemptingPress = 0;
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

            foreach (var button in _group.InternalButtons)
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

            if (!Disabled && !MuteSounds)
            {
                UserInterfaceManager.HoverSound();
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

            var drawMode = DrawMode;
            _beingHovered = false;
            if (drawMode != DrawMode)
            {
                DrawModeChanged();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Group = null;
        }

        public enum DrawModeEnum : byte
        {
            Normal = 0,
            Pressed = 1,
            Hover = 2,
            Disabled = 3
        }

        [Virtual]
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
        public sealed class ButtonToggledEventArgs : ButtonEventArgs
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
        /// <summary>
        /// Whether it is legal for this button group to have no selected button.
        /// </summary>
        /// <remarks>
        /// If true, it's legal for no button in the group to be active.
        /// This is then the initial state of a new group of buttons (no button is automatically selected),
        /// and it becomes legal to manually clear the active button through code.
        /// The user cannot manually unselect the active button regardless, only by selecting a difference button.
        /// </remarks>
        public bool IsNoneSetAllowed { get; }

        /// <summary>
        /// Create a new <see cref="ButtonGroup"/>
        /// </summary>
        /// <param name="isNoneSetAllowed">The value of <see cref="IsNoneSetAllowed"/> on the new button group.</param>
        public ButtonGroup(bool isNoneSetAllowed = true)
        {
            IsNoneSetAllowed = isNoneSetAllowed;
        }

        internal readonly List<BaseButton> InternalButtons = new();
        public IReadOnlyList<BaseButton> Buttons => InternalButtons;

        public BaseButton? Pressed => InternalButtons.FirstOrDefault(x => x.Pressed);
    }
}
