using System.Collections.Generic;
using System.Runtime.InteropServices;
using GlfwKey = OpenToolkit.GraphicsLibraryFramework.Keys;
using GlfwButton = OpenToolkit.GraphicsLibraryFramework.MouseButton;

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

        public static Button ConvertGlfwButton(GlfwButton button)
        {
            return _openTKButtonMap[button];
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

        private static readonly Dictionary<GlfwButton, Button> _openTKButtonMap = new()
        {
            {GlfwButton.Left, Button.Left},
            {GlfwButton.Middle, Button.Middle},
            {GlfwButton.Right, Button.Right},
            {GlfwButton.Button4, Button.Button4},
            {GlfwButton.Button5, Button.Button5},
            {GlfwButton.Button6, Button.Button6},
            {GlfwButton.Button7, Button.Button7},
            {GlfwButton.Button8, Button.Button8},
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

        internal static Key ConvertGlfwKey(GlfwKey key)
        {
            if (_glfwKeyMap.TryGetValue(key, out var result))
            {
                return result;
            }

            return Key.Unknown;
        }

        internal static GlfwKey ConvertGlfwKeyReverse(Key key)
        {
            if (_glfwKeyMapReverse.TryGetValue(key, out var result))
            {
                return result;
            }

            return GlfwKey.Unknown;
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

        private static readonly Dictionary<GlfwKey, Key> _glfwKeyMap;
        private static readonly Dictionary<Key, GlfwKey> _glfwKeyMapReverse;

        private static readonly Dictionary<Key, string> _keyNiceNameMap;

        static Keyboard()
        {
            _glfwKeyMap = new Dictionary<GlfwKey, Key>
            {
                {GlfwKey.A, Key.A},
                {GlfwKey.B, Key.B},
                {GlfwKey.C, Key.C},
                {GlfwKey.D, Key.D},
                {GlfwKey.E, Key.E},
                {GlfwKey.F, Key.F},
                {GlfwKey.G, Key.G},
                {GlfwKey.H, Key.H},
                {GlfwKey.I, Key.I},
                {GlfwKey.J, Key.J},
                {GlfwKey.K, Key.K},
                {GlfwKey.L, Key.L},
                {GlfwKey.M, Key.M},
                {GlfwKey.N, Key.N},
                {GlfwKey.O, Key.O},
                {GlfwKey.P, Key.P},
                {GlfwKey.Q, Key.Q},
                {GlfwKey.R, Key.R},
                {GlfwKey.S, Key.S},
                {GlfwKey.T, Key.T},
                {GlfwKey.U, Key.U},
                {GlfwKey.V, Key.V},
                {GlfwKey.W, Key.W},
                {GlfwKey.X, Key.X},
                {GlfwKey.Y, Key.Y},
                {GlfwKey.Z, Key.Z},
                {GlfwKey.D0, Key.Num0},
                {GlfwKey.D1, Key.Num1},
                {GlfwKey.D2, Key.Num2},
                {GlfwKey.D3, Key.Num3},
                {GlfwKey.D4, Key.Num4},
                {GlfwKey.D5, Key.Num5},
                {GlfwKey.D6, Key.Num6},
                {GlfwKey.D7, Key.Num7},
                {GlfwKey.D8, Key.Num8},
                {GlfwKey.D9, Key.Num9},
                {GlfwKey.KeyPad0, Key.NumpadNum0},
                {GlfwKey.KeyPad1, Key.NumpadNum1},
                {GlfwKey.KeyPad2, Key.NumpadNum2},
                {GlfwKey.KeyPad3, Key.NumpadNum3},
                {GlfwKey.KeyPad4, Key.NumpadNum4},
                {GlfwKey.KeyPad5, Key.NumpadNum5},
                {GlfwKey.KeyPad6, Key.NumpadNum6},
                {GlfwKey.KeyPad7, Key.NumpadNum7},
                {GlfwKey.KeyPad8, Key.NumpadNum8},
                {GlfwKey.KeyPad9, Key.NumpadNum9},
                {GlfwKey.Escape, Key.Escape},
                {GlfwKey.LeftControl, Key.Control},
                {GlfwKey.RightControl, Key.Control},
                {GlfwKey.RightShift, Key.Shift},
                {GlfwKey.LeftShift, Key.Shift},
                {GlfwKey.LeftAlt, Key.Alt},
                {GlfwKey.RightAlt, Key.Alt},
                {GlfwKey.LeftSuper, Key.LSystem},
                {GlfwKey.RightSuper, Key.RSystem},
                {GlfwKey.Menu, Key.Menu},
                {GlfwKey.LeftBracket, Key.LBracket},
                {GlfwKey.RightBracket, Key.RBracket},
                {GlfwKey.Semicolon, Key.SemiColon},
                {GlfwKey.Comma, Key.Comma},
                {GlfwKey.Period, Key.Period},
                {GlfwKey.Apostrophe, Key.Apostrophe},
                {GlfwKey.Slash, Key.Slash},
                {GlfwKey.Backslash, Key.BackSlash},
                {GlfwKey.GraveAccent, Key.Tilde},
                {GlfwKey.Equal, Key.Equal},
                {GlfwKey.Space, Key.Space},
                {GlfwKey.Enter, Key.Return},
                {GlfwKey.KeyPadEnter, Key.NumpadEnter},
                {GlfwKey.Backspace, Key.BackSpace},
                {GlfwKey.Tab, Key.Tab},
                {GlfwKey.PageUp, Key.PageUp},
                {GlfwKey.PageDown, Key.PageDown},
                {GlfwKey.End, Key.End},
                {GlfwKey.Home, Key.Home},
                {GlfwKey.Insert, Key.Insert},
                {GlfwKey.Delete, Key.Delete},
                {GlfwKey.Minus, Key.Minus},
                {GlfwKey.KeyPadAdd, Key.NumpadAdd},
                {GlfwKey.KeyPadSubtract, Key.NumpadSubtract},
                {GlfwKey.KeyPadDivide, Key.NumpadDivide},
                {GlfwKey.KeyPadMultiply, Key.NumpadMultiply},
                {GlfwKey.KeyPadDecimal, Key.NumpadDecimal},
                {GlfwKey.Left, Key.Left},
                {GlfwKey.Right, Key.Right},
                {GlfwKey.Up, Key.Up},
                {GlfwKey.Down, Key.Down},
                {GlfwKey.F1, Key.F1},
                {GlfwKey.F2, Key.F2},
                {GlfwKey.F3, Key.F3},
                {GlfwKey.F4, Key.F4},
                {GlfwKey.F5, Key.F5},
                {GlfwKey.F6, Key.F6},
                {GlfwKey.F7, Key.F7},
                {GlfwKey.F8, Key.F8},
                {GlfwKey.F9, Key.F9},
                {GlfwKey.F10, Key.F10},
                {GlfwKey.F11, Key.F11},
                {GlfwKey.F12, Key.F12},
                {GlfwKey.F13, Key.F13},
                {GlfwKey.F14, Key.F14},
                {GlfwKey.F15, Key.F15},
                {GlfwKey.Pause, Key.Pause},
            };

            _glfwKeyMapReverse = new Dictionary<Key, GlfwKey>();

            foreach (var (key, value) in _glfwKeyMap)
            {
                _glfwKeyMapReverse[value] = key;
            }

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
