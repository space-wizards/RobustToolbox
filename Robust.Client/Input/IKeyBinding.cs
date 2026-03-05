using Robust.Shared.Input;

namespace Robust.Client.Input
{
    [NotContentImplementable]
    public interface IKeyBinding
    {
        BoundKeyState State { get; }
        BoundKeyFunction Function { get; }
        string FunctionCommand { get; }
        KeyBindingType BindingType { get; }

        Keyboard.Key BaseKey { get; }
        Keyboard.Key Mod1 { get; }
        Keyboard.Key Mod2 { get; }
        Keyboard.Key Mod3 { get; }

        bool CanFocus { get; }
        bool CanRepeat { get; }
        bool AllowSubCombs { get; }

        /// <summary>
        ///     For a <see cref="KeyBindingType.Command"/>-type binding,
        ///     whether the binding should activate if UI is focused.
        /// </summary>
        bool CommandWhenUIFocused { get; }
        int Priority { get; }

        /// <summary>
        ///     Gets a user-presentable, localized & keyboard-adjusted string for which buttons the user has to press.
        /// </summary>
        string GetKeyString();
    }
}
