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
            Left = GD.BUTTON_LEFT,
            Middle = GD.BUTTON_MIDDLE,
            Right = GD.BUTTON_RIGHT,
        }

        /// <summary>
        ///     Represents mouse buttons, but in bitflag form.
        /// </summary>
        [Flags]
        public enum ButtonMask
        {
            None = 0,
            Left = GD.BUTTON_MASK_LEFT,
            Middle = GD.BUTTON_MASK_MIDDLE,
            Right = GD.BUTTON_MASK_RIGHT,
        }

        /// <summary>
        ///     Represents mousewheel directions.
        /// </summary>
        public enum Wheel
        {
            Up = GD.BUTTON_WHEEL_UP,
            Down = GD.BUTTON_WHEEL_DOWN,
            Left = GD.BUTTON_WHEEL_LEFT,
            Right = GD.BUTTON_WHEEL_RIGHT,
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
        public static bool IsKeyPrintable(Key key) => ((int)key & GD.SPKEY) == 0;

        /// <summary>
        ///     Represents a key on the keyboard.
        /// </summary>
        public enum Key : int
        {
            A = GD.KEY_A,
            B = GD.KEY_B,
            C = GD.KEY_C,
            D = GD.KEY_D,
            E = GD.KEY_E,
            F = GD.KEY_F,
            G = GD.KEY_G,
            H = GD.KEY_H,
            I = GD.KEY_I,
            J = GD.KEY_J,
            K = GD.KEY_K,
            L = GD.KEY_L,
            M = GD.KEY_M,
            N = GD.KEY_N,
            O = GD.KEY_O,
            P = GD.KEY_P,
            Q = GD.KEY_Q,
            R = GD.KEY_R,
            S = GD.KEY_S,
            T = GD.KEY_T,
            U = GD.KEY_U,
            V = GD.KEY_V,
            W = GD.KEY_W,
            X = GD.KEY_X,
            Y = GD.KEY_Y,
            Z = GD.KEY_Z,
            Num0 = GD.KEY_0,
            Num1 = GD.KEY_1,
            Num2 = GD.KEY_2,
            Num3 = GD.KEY_3,
            Num4 = GD.KEY_4,
            Num5 = GD.KEY_5,
            Num6 = GD.KEY_6,
            Num7 = GD.KEY_7,
            Num8 = GD.KEY_8,
            Num9 = GD.KEY_9,
            NumpadNum0 = GD.KEY_KP_0,
            NumpadNum1 = GD.KEY_KP_1,
            NumpadNum2 = GD.KEY_KP_2,
            NumpadNum3 = GD.KEY_KP_3,
            NumpadNum4 = GD.KEY_KP_4,
            NumpadNum5 = GD.KEY_KP_5,
            NumpadNum6 = GD.KEY_KP_6,
            NumpadNum7 = GD.KEY_KP_7,
            NumpadNum8 = GD.KEY_KP_8,
            NumpadNum9 = GD.KEY_KP_9,
            Escape = GD.KEY_ESCAPE,
            Control = GD.KEY_CONTROL,
            Shift = GD.KEY_SHIFT,
            Alt = GD.KEY_ALT,
            LSystem = GD.KEY_SUPER_L,
            RSystem = GD.KEY_SUPER_R,
            Menu = GD.KEY_MENU,
            LBracket = GD.KEY_BRACKETLEFT,
            RBracket = GD.KEY_BRACKETRIGHT,
            SemiColon = GD.KEY_SEMICOLON,
            Comma = GD.KEY_COMMA,
            Period = GD.KEY_PERIOD,
            Quote = GD.KEY_QUOTELEFT,
            Slash = GD.KEY_SLASH,
            BackSlash = GD.KEY_BACKSLASH,
            Tilde = GD.KEY_ASCIITILDE,
            Equal = GD.KEY_EQUAL,
            Dash = GD.KEY_HYPHEN,
            Space = GD.KEY_SPACE,
            Return = GD.KEY_ENTER,
            NumpadEnter = GD.KEY_KP_ENTER,
            BackSpace = GD.KEY_BACKSPACE,
            Tab = GD.KEY_TAB,
            PageUp = GD.KEY_PAGEUP,
            PageDown = GD.KEY_PAGEDOWN,
            End = GD.KEY_END,
            Home = GD.KEY_HOME,
            Insert = GD.KEY_INSERT,
            Delete = GD.KEY_DELETE,
            Plus = GD.KEY_PLUS,
            Minus = GD.KEY_MINUS,
            Asterisk = GD.KEY_ASTERISK,
            NumpadAdd = GD.KEY_KP_ADD,
            NumpadSubtract = GD.KEY_KP_SUBTRACT,
            NumpadDivide = GD.KEY_KP_DIVIDE,
            NumpadMultiply = GD.KEY_KP_MULTIPLY,
            Left = GD.KEY_LEFT,
            Right = GD.KEY_RIGHT,
            Up = GD.KEY_UP,
            Down = GD.KEY_DOWN,
            F1 = GD.KEY_F1,
            F2 = GD.KEY_F2,
            F3 = GD.KEY_F3,
            F4 = GD.KEY_F4,
            F5 = GD.KEY_F5,
            F6 = GD.KEY_F6,
            F7 = GD.KEY_F7,
            F8 = GD.KEY_F8,
            F9 = GD.KEY_F9,
            F10 = GD.KEY_F10,
            F11 = GD.KEY_F11,
            F12 = GD.KEY_F12,
            F13 = GD.KEY_F13,
            F14 = GD.KEY_F14,
            F15 = GD.KEY_F15,
            Pause = GD.KEY_PAUSE,
        }
    }
}
