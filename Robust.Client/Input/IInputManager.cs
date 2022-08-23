using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Robust.Client.Input
{
    /// <summary>
    ///     Manages key bindings, input commands and other misc. input systems.
    /// </summary>
    public interface IInputManager
    {
        bool Enabled { get; set; }

        /// <summary>
        ///     Relay a key event that hit a viewport further down the input stack.
        /// </summary>
        /// <remarks>
        ///     This is ONLY intended to be used in key handler from viewport controls.
        /// </remarks>
        /// <param name="control">The viewport control that relayed the input.</param>
        /// <param name="eventArgs">The key event args triggering the input.</param>
        void ViewportKeyEvent(Control? control, BoundKeyEventArgs eventArgs);

        /// <summary>
        ///     The position and window of the mouse.
        /// </summary>
        ScreenCoordinates MouseScreenPosition { get; }

        BoundKeyMap NetworkBindMap { get; }

        IInputContextContainer Contexts { get; }

        void Initialize();
        void SaveToUserData();

        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);

        IKeyBinding RegisterBinding(in KeyBindingRegistration reg, bool markModified=true);

        void RemoveBinding(IKeyBinding binding, bool markModified=true);

        /// <summary>
        ///     Gets a key binding according to the function it is bound to.
        /// </summary>
        /// <remarks>
        ///     Which keybind is returned if there are multiple for a key function is unspecified.
        /// </remarks>
        /// <param name="function">The function the key binding is bound to.</param>
        /// <returns>The key binding.</returns>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown if no there is no keybind for the specified function.
        /// </exception>
        IKeyBinding GetKeyBinding(BoundKeyFunction function);

        /// <remarks>
        ///     Which keybind is returned if there are multiple for a key function is unspecified.
        /// </remarks>
        bool TryGetKeyBinding(BoundKeyFunction function, [NotNullWhen(true)] out IKeyBinding? binding);

        /// <summary>
        ///     Returns the input command bound to a key function.
        /// </summary>
        /// <param name="function">The key function to find the bound input command for.</param>
        /// <returns>An input command, if any. Null if no command is set.</returns>
        InputCmdHandler? GetInputCommand(BoundKeyFunction function);

        void SetInputCommand(BoundKeyFunction function, InputCmdHandler? cmdHandler);

        /// <summary>
        ///     UIKeyBindStateChanged is called when a keybind is found.
        /// </summary>
        event Func<BoundKeyEventArgs, bool>? UIKeyBindStateChanged;

        /// <summary>
        ///     If UIKeyBindStateChanged did not handle the BoundKeyEvent, KeyBindStateChanged is called.
        /// </summary>
        event Action<ViewportBoundKeyEventArgs>? KeyBindStateChanged;

        IEnumerable<BoundKeyFunction> DownKeyFunctions { get; }

        /// <summary>
        ///     Gets the name of the key on the keyboard, based on the current input method and language.
        /// </summary>
        string GetKeyName(Keyboard.Key key);

        /// <summary>
        ///     Gets a user-presentable, localized & keyboard-adjusted string for which combination
        ///     of buttons is bound to the specified key function.
        /// </summary>
        string GetKeyFunctionButtonString(BoundKeyFunction function);

        /// <summary>
        ///     Gets all the key bindings currently registered into the manager.
        /// </summary>
        IEnumerable<IKeyBinding> AllBindings { get; }

        /// <summary>
        ///     An event that gets fired before everything else when a key event comes in.
        ///     For key down events, the event can be handled to block it.
        /// </summary>
        /// <remarks>
        ///     Do not use this for regular input handling.
        ///     This is a low-level API intended solely for stuff like the key rebinding menu.
        /// </remarks>
        event KeyEventAction FirstChanceOnKeyEvent;

        event Action<IKeyBinding> OnKeyBindingAdded;
        event Action<IKeyBinding> OnKeyBindingRemoved;
        event Action OnInputModeChanged;

        /// <summary>
        ///     Gets all the keybinds bound to a specific function.
        /// </summary>
        IReadOnlyList<IKeyBinding> GetKeyBindings(BoundKeyFunction function);

        /// <summary>
        ///     Resets the bindings for a specific BoundKeyFunction to the defaults from the resource files.
        /// </summary>
        /// <param name="function">The key function to reset the bindings for.</param>
        void ResetBindingsFor(BoundKeyFunction function);

        /// <summary>
        ///     Resets ALL the keybinds to the defaults from the resource files.
        /// </summary>
        void ResetAllBindings();

        bool IsKeyFunctionModified(BoundKeyFunction function);

        bool IsKeyDown(Keyboard.Key key);

        void InputModeChanged();
    }
}
