using System;
using System.Collections.Generic;
using TKKey = OpenTK.Input.Key;
using TKButton = OpenTK.Input.MouseButton;

namespace Robust.Client.Input
{
    public static class Mouse
    {
        /// <summary>
        ///     Represents one of three mouse buttons.
        /// </summary>
        public enum Button
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

        /// <summary>
        ///     Represents mouse buttons, but in bitflag form.
        /// </summary>
        [Flags]
        public enum ButtonMask
        {
            // These match Godot's
            None = 0,
            Left = 1,
            Middle = 2,
            Right = 4,
        }

        /// <summary>
        ///     Represents mousewheel directions.
        /// </summary>
        public enum Wheel
        {
            // These match Godot's
            Up = 4,
            Down = 5,
            Left = 6,
            Right = 7,
        }

        public static Keyboard.Key MouseButtonToKey(Button button)
        {
            return _mouseKeyMap[button];
        }

        public static Button ConvertOpenTKButton(OpenTK.Input.MouseButton button)
        {
            return _openTKButtonMap[button];
        }

        private static readonly Dictionary<Button, Keyboard.Key> _mouseKeyMap = new Dictionary<Button, Keyboard.Key>
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

        private static readonly Dictionary<TKButton, Button> _openTKButtonMap = new Dictionary<TKButton, Button>
        {
            {TKButton.Left, Button.Left},
            {TKButton.Middle, Button.Middle},
            {TKButton.Right, Button.Right},
            {TKButton.Button4, Button.Button4},
            {TKButton.Button5, Button.Button5},
            {TKButton.Button6, Button.Button6},
            {TKButton.Button7, Button.Button7},
            {TKButton.Button8, Button.Button8},
            {TKButton.Button9, Button.Button9},
            {TKButton.LastButton, Button.LastButton},
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
            Quote,
            Apostrophe,
            Slash,
            BackSlash,
            Tilde,
            Equal,
            Dash,
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
            Plus,
            Minus,
            Asterisk,
            NumpadAdd,
            NumpadSubtract,
            NumpadDivide,
            NumpadMultiply,
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
        
        internal static Key ConvertOpenTKKey(TKKey key)
        {
            if (OpenTKKeyMap.TryGetValue(key, out var result))
            {
                return result;
            }

            return Key.Unknown;
        }

        private static readonly Dictionary<TKKey, Key> OpenTKKeyMap = new Dictionary<OpenTK.Input.Key, Key>
        {
            // TODO: Missing keys OpenTK has but we don't:
            // Scroll Lock, Caps Lock, Print Screen, Num Lock, Clear, Sleep, F keys above 15, NonUSBackSlash, LastKey.
            {TKKey.Unknown, Key.Unknown},
            {TKKey.LShift, Key.Shift},
            {TKKey.RShift, Key.Shift},
            {TKKey.LControl, Key.Control},
            {TKKey.RControl, Key.Control},
            {TKKey.LAlt, Key.Alt},
            {TKKey.RAlt, Key.Alt},
            {TKKey.LWin, Key.LSystem},
            {TKKey.RWin, Key.RSystem},
            {TKKey.Menu, Key.Menu},
            {TKKey.F1, Key.F1},
            {TKKey.F2, Key.F2},
            {TKKey.F3, Key.F3},
            {TKKey.F4, Key.F4},
            {TKKey.F5, Key.F5},
            {TKKey.F6, Key.F6},
            {TKKey.F7, Key.F7},
            {TKKey.F8, Key.F8},
            {TKKey.F9, Key.F9},
            {TKKey.F10, Key.F10},
            {TKKey.F11, Key.F11},
            {TKKey.F12, Key.F12},
            {TKKey.F13, Key.F13},
            {TKKey.F14, Key.F14},
            {TKKey.F15, Key.F15},
            {TKKey.Up, Key.Up},
            {TKKey.Down, Key.Down},
            {TKKey.Left, Key.Left},
            {TKKey.Right, Key.Right},
            {TKKey.Enter, Key.Return},
            {TKKey.Escape, Key.Escape},
            {TKKey.Space, Key.Space},
            {TKKey.Tab, Key.Tab},
            {TKKey.Back, Key.BackSpace},
            {TKKey.Insert, Key.Insert},
            {TKKey.Delete, Key.Delete},
            {TKKey.PageUp, Key.PageUp},
            {TKKey.PageDown, Key.PageDown},
            {TKKey.Home, Key.Home},
            {TKKey.End, Key.End},
            {TKKey.Pause, Key.Pause},
            {TKKey.Keypad0, Key.NumpadNum0},
            {TKKey.Keypad1, Key.NumpadNum1},
            {TKKey.Keypad2, Key.NumpadNum2},
            {TKKey.Keypad3, Key.NumpadNum3},
            {TKKey.Keypad4, Key.NumpadNum4},
            {TKKey.Keypad5, Key.NumpadNum5},
            {TKKey.Keypad6, Key.NumpadNum6},
            {TKKey.Keypad7, Key.NumpadNum7},
            {TKKey.Keypad8, Key.NumpadNum8},
            {TKKey.Keypad9, Key.NumpadNum9},
            {TKKey.KeypadDivide, Key.NumpadDivide},
            {TKKey.KeypadMultiply, Key.NumpadMultiply},
            {TKKey.KeypadMinus, Key.Minus},
            {TKKey.KeypadAdd, Key.NumpadAdd},
            {TKKey.KeypadEnter, Key.NumpadEnter},
            {TKKey.A, Key.A},
            {TKKey.B, Key.B},
            {TKKey.C, Key.C},
            {TKKey.D, Key.D},
            {TKKey.E, Key.E},
            {TKKey.F, Key.F},
            {TKKey.G, Key.G},
            {TKKey.H, Key.H},
            {TKKey.I, Key.I},
            {TKKey.J, Key.J},
            {TKKey.K, Key.K},
            {TKKey.L, Key.L},
            {TKKey.M, Key.M},
            {TKKey.N, Key.N},
            {TKKey.O, Key.O},
            {TKKey.P, Key.P},
            {TKKey.Q, Key.Q},
            {TKKey.R, Key.R},
            {TKKey.S, Key.S},
            {TKKey.T, Key.T},
            {TKKey.U, Key.U},
            {TKKey.V, Key.V},
            {TKKey.W, Key.W},
            {TKKey.X, Key.X},
            {TKKey.Y, Key.Y},
            {TKKey.Z, Key.Z},
            {TKKey.Number0, Key.Num0},
            {TKKey.Number1, Key.Num1},
            {TKKey.Number2, Key.Num2},
            {TKKey.Number3, Key.Num3},
            {TKKey.Number4, Key.Num4},
            {TKKey.Number5, Key.Num5},
            {TKKey.Number6, Key.Num6},
            {TKKey.Number7, Key.Num7},
            {TKKey.Number8, Key.Num8},
            {TKKey.Number9, Key.Num9},
            {TKKey.Tilde, Key.Tilde},
            {TKKey.Minus, Key.Minus},
            {TKKey.Plus, Key.Plus},
            {TKKey.LBracket, Key.LBracket},
            {TKKey.RBracket, Key.RBracket},
            {TKKey.Semicolon, Key.SemiColon},
            {TKKey.Quote, Key.Quote},
            {TKKey.Comma, Key.Comma},
            {TKKey.Period, Key.Period},
            {TKKey.Slash, Key.Slash},
            {TKKey.BackSlash, Key.BackSlash},
        };
    }
}
