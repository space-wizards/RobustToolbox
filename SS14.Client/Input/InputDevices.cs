using Godot;
using System;

namespace SS14.Client.Input
{
    public static class Mouse
    {
        public static bool IsButtonPressed(Button button) => Godot.Input.IsMouseButtonPressed((int)button);

        // TODO: People will definitely want support for extra mouse buttons,
        //         Godot doesn't seem to support this though.
        /// <summary>
        ///     Represents one of three mouse buttons.
        /// </summary>
        public enum Button
        {
            Left = ButtonList.Left,
            Middle = ButtonList.Middle,
            Right = ButtonList.Right,
        }

        /// <summary>
        ///     Represents mouse buttons, but in bitflag form.
        /// </summary>
        [Flags]
        public enum ButtonMask
        {
            None = 0,
            Left = Godot.ButtonList.MaskLeft,
            Middle = Godot.ButtonList.MaskMiddle,
            Right = Godot.ButtonList.MaskRight,
        }

        /// <summary>
        ///     Represents mousewheel directions.
        /// </summary>
        public enum Wheel
        {
            Up = Godot.ButtonList.WheelUp,
            Down = Godot.ButtonList.WheelDown,
            Left = Godot.ButtonList.WheelLeft,
            Right = Godot.ButtonList.WheelRight,
        }
    }

    public static class Keyboard
    {
        /// <summary>
        ///     Checks whether the provided key on the keyboard is currently held down.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>True if the provided key is currently held down, false otherwise.</returns>
        public static bool IsKeyPressed(Key key) => Godot.Input.IsKeyPressed((int)key);

        /// <summary>
        ///     Checks whether a key is printable.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key is printable, false otherwise.</returns>
        // See Godot docs: SPKEY = 16777216 — Scancodes with this bit applied are non printable.
        public static bool IsKeyPrintable(Key key) => ((int)key & GD.Spkey) == 0;

        /// <summary>
        ///     Represents a key on the keyboard.
        /// </summary>
        public enum Key : int
        {
            A = Godot.KeyList.A,
            B = Godot.KeyList.B,
            C = Godot.KeyList.C,
            D = Godot.KeyList.D,
            E = Godot.KeyList.E,
            F = Godot.KeyList.F,
            G = Godot.KeyList.G,
            H = Godot.KeyList.H,
            I = Godot.KeyList.I,
            J = Godot.KeyList.J,
            K = Godot.KeyList.K,
            L = Godot.KeyList.L,
            M = Godot.KeyList.M,
            N = Godot.KeyList.N,
            O = Godot.KeyList.O,
            P = Godot.KeyList.P,
            Q = Godot.KeyList.Q,
            R = Godot.KeyList.R,
            S = Godot.KeyList.S,
            T = Godot.KeyList.T,
            U = Godot.KeyList.U,
            V = Godot.KeyList.V,
            W = Godot.KeyList.W,
            X = Godot.KeyList.X,
            Y = Godot.KeyList.Y,
            Z = Godot.KeyList.Z,
            Num0 = Godot.KeyList.Key0,
            Num1 = Godot.KeyList.Key1,
            Num2 = Godot.KeyList.Key2,
            Num3 = Godot.KeyList.Key3,
            Num4 = Godot.KeyList.Key4,
            Num5 = Godot.KeyList.Key5,
            Num6 = Godot.KeyList.Key6,
            Num7 = Godot.KeyList.Key7,
            Num8 = Godot.KeyList.Key8,
            Num9 = Godot.KeyList.Key9,
            NumpadNum0 = Godot.KeyList.Kp0,
            NumpadNum1 = Godot.KeyList.Kp1,
            NumpadNum2 = Godot.KeyList.Kp2,
            NumpadNum3 = Godot.KeyList.Kp3,
            NumpadNum4 = Godot.KeyList.Kp4,
            NumpadNum5 = Godot.KeyList.Kp5,
            NumpadNum6 = Godot.KeyList.Kp6,
            NumpadNum7 = Godot.KeyList.Kp7,
            NumpadNum8 = Godot.KeyList.Kp8,
            NumpadNum9 = Godot.KeyList.Kp9,
            Escape = Godot.KeyList.Escape,
            Control = Godot.KeyList.Control,
            Shift = Godot.KeyList.Shift,
            Alt = Godot.KeyList.Alt,
            LSystem = Godot.KeyList.SuperL,
            RSystem = Godot.KeyList.SuperR,
            Menu = Godot.KeyList.Menu,
            LBracket = Godot.KeyList.Bracketleft,
            RBracket = Godot.KeyList.Bracketright,
            SemiColon = Godot.KeyList.Semicolon,
            Comma = Godot.KeyList.Comma,
            Period = Godot.KeyList.Period,
            /// Seems to be grave (under tilde).
            Quote = Godot.KeyList.Quoteleft,
            Apostrophe = Godot.KeyList.Apostrophe,
            Slash = Godot.KeyList.Slash,
            BackSlash = Godot.KeyList.Backslash,
            Tilde = Godot.KeyList.Asciitilde,
            Equal = Godot.KeyList.Equal,
            Dash = Godot.KeyList.Hyphen,
            Space = Godot.KeyList.Space,
            Return = Godot.KeyList.Enter,
            NumpadEnter = Godot.KeyList.KpEnter,
            BackSpace = Godot.KeyList.Backspace,
            Tab = Godot.KeyList.Tab,
            PageUp = Godot.KeyList.Pageup,
            PageDown = Godot.KeyList.Pagedown,
            End = Godot.KeyList.End,
            Home = Godot.KeyList.Home,
            Insert = Godot.KeyList.Insert,
            Delete = Godot.KeyList.Delete,
            Plus = Godot.KeyList.Plus,
            Minus = Godot.KeyList.Minus,
            Asterisk = Godot.KeyList.Asterisk,
            NumpadAdd = Godot.KeyList.KpAdd,
            NumpadSubtract = Godot.KeyList.KpSubtract,
            NumpadDivide = Godot.KeyList.KpDivide,
            NumpadMultiply = Godot.KeyList.KpMultiply,
            Left = Godot.KeyList.Left,
            Right = Godot.KeyList.Right,
            Up = Godot.KeyList.Up,
            Down = Godot.KeyList.Down,
            F1 = Godot.KeyList.F1,
            F2 = Godot.KeyList.F2,
            F3 = Godot.KeyList.F3,
            F4 = Godot.KeyList.F4,
            F5 = Godot.KeyList.F5,
            F6 = Godot.KeyList.F6,
            F7 = Godot.KeyList.F7,
            F8 = Godot.KeyList.F8,
            F9 = Godot.KeyList.F9,
            F10 = Godot.KeyList.F10,
            F11 = Godot.KeyList.F11,
            F12 = Godot.KeyList.F12,
            F13 = Godot.KeyList.F13,
            F14 = Godot.KeyList.F14,
            F15 = Godot.KeyList.F15,
            Pause = Godot.KeyList.Pause,
        }
    }
}
