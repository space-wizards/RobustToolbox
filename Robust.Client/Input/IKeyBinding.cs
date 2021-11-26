using Robust.Shared.Input;

namespace Robust.Client.Input
{
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
        int Priority { get; }

        /// <summary>
        ///     Gets a user-presentable, localized & keyboard-adjusted string for which buttons the user has to press.
        /// </summary>
        string GetKeyString();
    }
}
