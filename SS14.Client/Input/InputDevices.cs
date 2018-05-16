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
            Unknown = 0,
            A = 1,
            B = 2,
            C = 3,
            D = 4,
            E = 5,
            F = 6,
            G = 7,
            H = 8,
            I = 9,
            J = 10,
            K = 11,
            L = 12,
            M = 13,
            N = 14,
            O = 15,
            P = 16,
            Q = 17,
            R = 18,
            S = 19,
            T = 20,
            U = 21,
            V = 22,
            W = 23,
            X = 24,
            Y = 25,
            Z = 26,
            Num0 = 27,
            Num1 = 28,
            Num2 = 29,
            Num3 = 30,
            Num4 = 31,
            Num5 = 32,
            Num6 = 33,
            Num7 = 34,
            Num8 = 35,
            Num9 = 36,
            NumpadNum0 = 37,
            NumpadNum1 = 38,
            NumpadNum2 = 39,
            NumpadNum3 = 40,
            NumpadNum4 = 41,
            NumpadNum5 = 42,
            NumpadNum6 = 43,
            NumpadNum7 = 44,
            NumpadNum8 = 45,
            NumpadNum9 = 46,
            Escape = 47,
            Control = 48,
            Shift = 49,
            Alt = 50,
            LSystem = 51,
            RSystem = 52,
            Menu = 53,
            LBracket = 54,
            RBracket = 55,
            SemiColon = 56,
            Comma = 57,
            Period = 58,
            Quote = 59,
            Apostrophe = 60,
            Slash = 61,
            BackSlash = 62,
            Tilde = 63,
            Equal = 64,
            Dash = 65,
            Space = 66,
            Return = 67,
            NumpadEnter = 68,
            BackSpace = 69,
            Tab = 70,
            PageUp = 71,
            PageDown = 72,
            End = 73,
            Home = 74,
            Insert = 75,
            Delete = 76,
            Plus = 77,
            Minus = 78,
            Asterisk = 79,
            NumpadAdd = 80,
            NumpadSubtract = 81,
            NumpadDivide = 82,
            NumpadMultiply = 83,
            Left = 84,
            Right = 85,
            Up = 86,
            Down = 87,
            F1 = 88,
            F2 = 89,
            F3 = 90,
            F4 = 91,
            F5 = 92,
            F6 = 93,
            F7 = 94,
            F8 = 95,
            F9 = 96,
            F10 = 97,
            F11 = 98,
            F12 = 99,
            F13 = 100,
            F14 = 101,
            F15 = 102,
            Pause = 103,
        }

        public static Key GonvertGodotKey(int key)
        {
            switch ((Godot.KeyList)key)
            {
                case Godot.KeyList.A:
                    return Key.A;
                case Godot.KeyList.B:
                    return Key.B;
                case Godot.KeyList.C:
                    return Key.C;
                case Godot.KeyList.D:
                    return Key.D;
                case Godot.KeyList.E:
                    return Key.E;
                case Godot.KeyList.F:
                    return Key.F;
                case Godot.KeyList.G:
                    return Key.G;
                case Godot.KeyList.H:
                    return Key.H;
                case Godot.KeyList.I:
                    return Key.I;
                case Godot.KeyList.J:
                    return Key.J;
                case Godot.KeyList.K:
                    return Key.K;
                case Godot.KeyList.L:
                    return Key.L;
                case Godot.KeyList.M:
                    return Key.M;
                case Godot.KeyList.N:
                    return Key.N;
                case Godot.KeyList.O:
                    return Key.O;
                case Godot.KeyList.P:
                    return Key.P;
                case Godot.KeyList.Q:
                    return Key.Q;
                case Godot.KeyList.R:
                    return Key.R;
                case Godot.KeyList.S:
                    return Key.S;
                case Godot.KeyList.T:
                    return Key.T;
                case Godot.KeyList.U:
                    return Key.U;
                case Godot.KeyList.V:
                    return Key.V;
                case Godot.KeyList.W:
                    return Key.W;
                case Godot.KeyList.X:
                    return Key.X;
                case Godot.KeyList.Y:
                    return Key.Y;
                case Godot.KeyList.Z:
                    return Key.Z;
                case Godot.KeyList.Key0:
                    return Key.Num0;
                case Godot.KeyList.Key1:
                    return Key.Num1;
                case Godot.KeyList.Key2:
                    return Key.Num2;
                case Godot.KeyList.Key3:
                    return Key.Num3;
                case Godot.KeyList.Key4:
                    return Key.Num4;
                case Godot.KeyList.Key5:
                    return Key.Num5;
                case Godot.KeyList.Key6:
                    return Key.Num6;
                case Godot.KeyList.Key7:
                    return Key.Num7;
                case Godot.KeyList.Key8:
                    return Key.Num8;
                case Godot.KeyList.Key9:
                    return Key.Num9;
                case Godot.KeyList.Kp0:
                    return Key.NumpadNum0;
                case Godot.KeyList.Kp1:
                    return Key.NumpadNum1;
                case Godot.KeyList.Kp2:
                    return Key.NumpadNum2;
                case Godot.KeyList.Kp3:
                    return Key.NumpadNum3;
                case Godot.KeyList.Kp4:
                    return Key.NumpadNum4;
                case Godot.KeyList.Kp5:
                    return Key.NumpadNum5;
                case Godot.KeyList.Kp6:
                    return Key.NumpadNum6;
                case Godot.KeyList.Kp7:
                    return Key.NumpadNum7;
                case Godot.KeyList.Kp8:
                    return Key.NumpadNum8;
                case Godot.KeyList.Kp9:
                    return Key.NumpadNum9;
                case Godot.KeyList.Escape:
                    return Key.Escape;
                case Godot.KeyList.Control:
                    return Key.Control;
                case Godot.KeyList.Shift:
                    return Key.Shift;
                case Godot.KeyList.Alt:
                    return Key.Alt;
                case Godot.KeyList.SuperL:
                    return Key.LSystem;
                case Godot.KeyList.SuperR:
                    return Key.RSystem;
                case Godot.KeyList.Menu:
                    return Key.Menu;
                case Godot.KeyList.Bracketleft:
                    return Key.LBracket;
                case Godot.KeyList.Bracketright:
                    return Key.RBracket;
                case Godot.KeyList.Semicolon:
                    return Key.SemiColon;
                case Godot.KeyList.Comma:
                    return Key.Comma;
                case Godot.KeyList.Period:
                    return Key.Period;
                case Godot.KeyList.Quoteleft:
                    return Key.Quote;
                case Godot.KeyList.Apostrophe:
                    return Key.Apostrophe;
                case Godot.KeyList.Slash:
                    return Key.Slash;
                case Godot.KeyList.Backslash:
                    return Key.BackSlash;
                case Godot.KeyList.Asciitilde:
                    return Key.Tilde;
                case Godot.KeyList.Equal:
                    return Key.Equal;
                case Godot.KeyList.Hyphen:
                    return Key.Dash;
                case Godot.KeyList.Space:
                    return Key.Space;
                case Godot.KeyList.Enter:
                    return Key.Return;
                case Godot.KeyList.KpEnter:
                    return Key.NumpadEnter;
                case Godot.KeyList.Backspace:
                    return Key.BackSpace;
                case Godot.KeyList.Tab:
                    return Key.Tab;
                case Godot.KeyList.Pageup:
                    return Key.PageUp;
                case Godot.KeyList.Pagedown:
                    return Key.PageDown;
                case Godot.KeyList.End:
                    return Key.End;
                case Godot.KeyList.Home:
                    return Key.Home;
                case Godot.KeyList.Insert:
                    return Key.Insert;
                case Godot.KeyList.Delete:
                    return Key.Delete;
                case Godot.KeyList.Plus:
                    return Key.Plus;
                case Godot.KeyList.Minus:
                    return Key.Minus;
                case Godot.KeyList.Asterisk:
                    return Key.Asterisk;
                case Godot.KeyList.KpAdd:
                    return Key.NumpadAdd;
                case Godot.KeyList.KpSubtract:
                    return Key.NumpadSubtract;
                case Godot.KeyList.KpDivide:
                    return Key.NumpadDivide;
                case Godot.KeyList.KpMultiply:
                    return Key.NumpadMultiply;
                case Godot.KeyList.Left:
                    return Key.Left;
                case Godot.KeyList.Right:
                    return Key.Right;
                case Godot.KeyList.Up:
                    return Key.Up;
                case Godot.KeyList.Down:
                    return Key.Down;
                case Godot.KeyList.F1:
                    return Key.F1;
                case Godot.KeyList.F2:
                    return Key.F2;
                case Godot.KeyList.F3:
                    return Key.F3;
                case Godot.KeyList.F4:
                    return Key.F4;
                case Godot.KeyList.F5:
                    return Key.F5;
                case Godot.KeyList.F6:
                    return Key.F6;
                case Godot.KeyList.F7:
                    return Key.F7;
                case Godot.KeyList.F8:
                    return Key.F8;
                case Godot.KeyList.F9:
                    return Key.F9;
                case Godot.KeyList.F10:
                    return Key.F10;
                case Godot.KeyList.F11:
                    return Key.F11;
                case Godot.KeyList.F12:
                    return Key.F12;
                case Godot.KeyList.F13:
                    return Key.F13;
                case Godot.KeyList.F14:
                    return Key.F14;
                case Godot.KeyList.F15:
                    return Key.F15;
                case Godot.KeyList.Pause:
                    return Key.Pause;
                default:
                    return Key.Unknown;
            }
        }
    }
}
