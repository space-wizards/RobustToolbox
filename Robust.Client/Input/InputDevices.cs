using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Robust.Client.Input
{
    public static class Mouse
    {
        /// <summary>
        ///     Represents one of three mouse buttons.
        /// </summary>
        public enum Button : byte
        {
            Left = 1,
            Middle = 2,
            Right = 3,
            Button4,
            Button5,
            Button6,
            Button7,
            Button8,
            Button9,
            LastButton,
        }

        public static Keyboard.Key MouseButtonToKey(Button button)
        {
            return _mouseKeyMap[button];
        }

        private static readonly Dictionary<Button, Keyboard.Key> _mouseKeyMap = new()
        {
            {Button.Left, Keyboard.Key.MouseLeft},
            {Button.Middle, Keyboard.Key.MouseMiddle},
            {Button.Right, Keyboard.Key.MouseRight},
            {Button.Button4, Keyboard.Key.MouseButton4},
            {Button.Button5, Keyboard.Key.MouseButton5},
            {Button.Button6, Keyboard.Key.MouseButton6},
            {Button.Button7, Keyboard.Key.MouseButton7},
            {Button.Button8, Keyboard.Key.MouseButton8},
            {Button.Button9, Keyboard.Key.MouseButton9},
            {Button.LastButton, Keyboard.Key.Unknown},
        };

    }

    public static class Keyboard
    {
        /// <summary>
        ///     Represents a key on the keyboard.
        /// </summary>
        // This enum HAS to be a byte for the input system bitflag fuckery to work.
        public enum Key : byte
        {
            Unknown = 0,
            MouseLeft,
            MouseRight,
            MouseMiddle,
            MouseButton4,
            MouseButton5,
            MouseButton6,
            MouseButton7,
            MouseButton8,
            MouseButton9,
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
            Q,
            R,
            S,
            T,
            U,
            V,
            W,
            X,
            Y,
            Z,
            Num0,
            Num1,
            Num2,
            Num3,
            Num4,
            Num5,
            Num6,
            Num7,
            Num8,
            Num9,
            NumpadNum0,
            NumpadNum1,
            NumpadNum2,
            NumpadNum3,
            NumpadNum4,
            NumpadNum5,
            NumpadNum6,
            NumpadNum7,
            NumpadNum8,
            NumpadNum9,
            Escape,
            Control,
            Shift,
            Alt,
            LSystem,
            RSystem,
            Menu,
            LBracket,
            RBracket,
            SemiColon,
            Comma,
            Period,
            Apostrophe,
            Slash,
            BackSlash,
            Tilde,
            Equal,
            Space,
            Return,
            NumpadEnter,
            BackSpace,
            Tab,
            PageUp,
            PageDown,
            End,
            Home,
            Insert,
            Delete,
            Minus,
            NumpadAdd,
            NumpadSubtract,
            NumpadDivide,
            NumpadMultiply,
            NumpadDecimal,
            Left,
            Right,
            Up,
            Down,
            F1,
            F2,
            F3,
            F4,
            F5,
            F6,
            F7,
            F8,
            F9,
            F10,
            F11,
            F12,
            F13,
            F14,
            F15,
            Pause,
        }

        public static bool IsMouseKey(this Key key)
        {
            return key >= Key.MouseLeft && key <= Key.MouseButton9;
        }

        /// <summary>
        ///     Gets a "nice" version of special unprintable keys such as <see cref="Key.Escape"/>.
        /// </summary>
        /// <returns><see langword="null"/> if there is no nice version of this special key.</returns>
        internal static string? GetSpecialKeyName(Key key)
        {
            if (_keyNiceNameMap.TryGetValue(key, out var val))
            {
                return val;
            }

            return null;
        }

        private static readonly Dictionary<Key, string> _keyNiceNameMap;

        static Keyboard()
        {

            _keyNiceNameMap = new Dictionary<Key, string>
            {
                {Key.Escape, "Escape"},
                {Key.Control, "Control"},
                {Key.Shift, "Shift"},
                {Key.Alt, "Alt"},
                {Key.Menu, "Menu"},
                {Key.F1, "F1"},
                {Key.F2, "F2"},
                {Key.F3, "F3"},
                {Key.F4, "F4"},
                {Key.F5, "F5"},
                {Key.F6, "F6"},
                {Key.F7, "F7"},
                {Key.F8, "F8"},
                {Key.F9, "F9"},
                {Key.F10, "F10"},
                {Key.F11, "F11"},
                {Key.F12, "F12"},
                {Key.F13, "F13"},
                {Key.F14, "F14"},
                {Key.F15, "F15"},
                {Key.Pause, "Pause"},
                {Key.Left, "Left"},
                {Key.Up, "Up"},
                {Key.Down, "Down"},
                {Key.Right, "Right"},
                {Key.Space, "Space"},
                {Key.Return, "Return"},
                {Key.NumpadEnter, "Num Enter"},
                {Key.BackSpace, "Backspace"},
                {Key.Tab, "Tab"},
                {Key.PageUp, "Page Up"},
                {Key.PageDown, "Page Down"},
                {Key.End, "End"},
                {Key.Home, "Home"},
                {Key.Insert, "Insert"},
                {Key.Delete, "Delete"},
                {Key.MouseLeft, "Mouse Left"},
                {Key.MouseRight, "Mouse Right"},
                {Key.MouseMiddle, "Mouse Middle"},
                {Key.MouseButton4, "Mouse 4"},
                {Key.MouseButton5, "Mouse 5"},
                {Key.MouseButton6, "Mouse 6"},
                {Key.MouseButton7, "Mouse 7"},
                {Key.MouseButton8, "Mouse 8"},
                {Key.MouseButton9, "Mouse 9"},
            };

            // Have to adjust system key name depending on platform.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _keyNiceNameMap.Add(Key.LSystem, "Left Cmd");
                _keyNiceNameMap.Add(Key.RSystem, "Right Cmd");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _keyNiceNameMap.Add(Key.LSystem, "Left Win");
                _keyNiceNameMap.Add(Key.RSystem, "Right Win");
            }
            else
            {
                _keyNiceNameMap.Add(Key.LSystem, "Left Meta");
                _keyNiceNameMap.Add(Key.RSystem, "Right Meta");
            }
        }
    }
}
