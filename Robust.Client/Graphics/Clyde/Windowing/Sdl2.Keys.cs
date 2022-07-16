using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Robust.Client.Input;
using Robust.Shared;
using static SDL2.SDL;
using static SDL2.SDL.SDL_Scancode;
using Key = Robust.Client.Input.Keyboard.Key;
using Button = Robust.Client.Input.Mouse.Button;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        // Indices are values of SDL_Scancode
        private static readonly Key[] KeyMap;
        private static readonly Dictionary<Key, SDL_Scancode> KeyMapReverse;
        private static readonly Button[] MouseButtonMap;

        // TODO: to avoid having to ask the windowing thread, key names are cached.
        private readonly Dictionary<Key, string> _printableKeyNameMap = new();

        private void InitKeyMap()
        {
            _printableKeyNameMap.Clear();
            // From GLFW's source code: this is the actual list of "printable" keys
            // that GetKeyName returns something for.

            for (var k = SDL_SCANCODE_A; k <= SDL_SCANCODE_0; k++)
            {
                CacheKey(k);
            }

            /*
            CacheKey(Keys.KeyPadEqual);
            for (var k = Keys.KeyPad0; k <= Keys.KeyPadAdd; k++)
            {
                CacheKey(k);
            }

            for (var k = Keys.Apostrophe; k <= Keys.World2; k++)
            {
                CacheKey(k);
            }
            */

            void CacheKey(SDL_Scancode scancode)
            {
                var rKey = ConvertSdl2Scancode(scancode);
                if (rKey == Key.Unknown)
                    return;

                string name;

                if (!_clyde._cfg.GetCVar(CVars.DisplayUSQWERTYHotkeys))
                {
                    name = SDL_GetKeyName(SDL_GetKeyFromScancode(scancode));
                }
                else
                {
                    name = scancode.ToString();
                }

                if (!string.IsNullOrEmpty(name))
                    _printableKeyNameMap.Add(rKey, name);
            }
        }

        public string KeyGetName(Key key)
        {
            if (_printableKeyNameMap.TryGetValue(key, out var name))
            {
                var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
                return textInfo.ToTitleCase(name);
            }

            name = Keyboard.GetSpecialKeyName(key, _loc);
            if (name != null)
                return _loc.GetString(name);

            return _loc.GetString("<unknown key>");
        }

        internal static Key ConvertSdl2Scancode(SDL_Scancode scancode)
        {
            return KeyMap[(int) scancode];
        }

        public static Button ConvertSdl2Button(int button)
        {
            return MouseButtonMap[button];
        }

        static Sdl2WindowingImpl()
        {
            MouseButtonMap = new Button[6];
            MouseButtonMap[SDL_BUTTON_LEFT] = Button.Left;
            MouseButtonMap[SDL_BUTTON_RIGHT] = Button.Right;
            MouseButtonMap[SDL_BUTTON_MIDDLE] = Button.Middle;
            MouseButtonMap[SDL_BUTTON_X1] = Button.Button4;
            MouseButtonMap[SDL_BUTTON_X2] = Button.Button5;

            KeyMap = new Key[(int) SDL_NUM_SCANCODES];
            MapKey(SDL_SCANCODE_A, Key.A);
            MapKey(SDL_SCANCODE_B, Key.B);
            MapKey(SDL_SCANCODE_C, Key.C);
            MapKey(SDL_SCANCODE_D, Key.D);
            MapKey(SDL_SCANCODE_E, Key.E);
            MapKey(SDL_SCANCODE_F, Key.F);
            MapKey(SDL_SCANCODE_G, Key.G);
            MapKey(SDL_SCANCODE_H, Key.H);
            MapKey(SDL_SCANCODE_I, Key.I);
            MapKey(SDL_SCANCODE_J, Key.J);
            MapKey(SDL_SCANCODE_K, Key.K);
            MapKey(SDL_SCANCODE_L, Key.L);
            MapKey(SDL_SCANCODE_M, Key.M);
            MapKey(SDL_SCANCODE_N, Key.N);
            MapKey(SDL_SCANCODE_O, Key.O);
            MapKey(SDL_SCANCODE_P, Key.P);
            MapKey(SDL_SCANCODE_Q, Key.Q);
            MapKey(SDL_SCANCODE_R, Key.R);
            MapKey(SDL_SCANCODE_S, Key.S);
            MapKey(SDL_SCANCODE_T, Key.T);
            MapKey(SDL_SCANCODE_U, Key.U);
            MapKey(SDL_SCANCODE_V, Key.V);
            MapKey(SDL_SCANCODE_W, Key.W);
            MapKey(SDL_SCANCODE_X, Key.X);
            MapKey(SDL_SCANCODE_Y, Key.Y);
            MapKey(SDL_SCANCODE_Z, Key.Z);
            MapKey(SDL_SCANCODE_0, Key.Num0);
            MapKey(SDL_SCANCODE_1, Key.Num1);
            MapKey(SDL_SCANCODE_2, Key.Num2);
            MapKey(SDL_SCANCODE_3, Key.Num3);
            MapKey(SDL_SCANCODE_4, Key.Num4);
            MapKey(SDL_SCANCODE_5, Key.Num5);
            MapKey(SDL_SCANCODE_6, Key.Num6);
            MapKey(SDL_SCANCODE_7, Key.Num7);
            MapKey(SDL_SCANCODE_8, Key.Num8);
            MapKey(SDL_SCANCODE_9, Key.Num9);
            MapKey(SDL_SCANCODE_KP_0, Key.NumpadNum0);
            MapKey(SDL_SCANCODE_KP_1, Key.NumpadNum1);
            MapKey(SDL_SCANCODE_KP_2, Key.NumpadNum2);
            MapKey(SDL_SCANCODE_KP_3, Key.NumpadNum3);
            MapKey(SDL_SCANCODE_KP_4, Key.NumpadNum4);
            MapKey(SDL_SCANCODE_KP_5, Key.NumpadNum5);
            MapKey(SDL_SCANCODE_KP_6, Key.NumpadNum6);
            MapKey(SDL_SCANCODE_KP_7, Key.NumpadNum7);
            MapKey(SDL_SCANCODE_KP_8, Key.NumpadNum8);
            MapKey(SDL_SCANCODE_KP_9, Key.NumpadNum9);
            MapKey(SDL_SCANCODE_ESCAPE, Key.Escape);
            MapKey(SDL_SCANCODE_LCTRL, Key.Control);
            MapKey(SDL_SCANCODE_RCTRL, Key.Control);
            MapKey(SDL_SCANCODE_RSHIFT, Key.Shift);
            MapKey(SDL_SCANCODE_LSHIFT, Key.Shift);
            MapKey(SDL_SCANCODE_LALT, Key.Alt);
            MapKey(SDL_SCANCODE_RALT, Key.Alt);
            MapKey(SDL_SCANCODE_LGUI, Key.LSystem);
            MapKey(SDL_SCANCODE_RGUI, Key.RSystem);
            MapKey(SDL_SCANCODE_MENU, Key.Menu);
            MapKey(SDL_SCANCODE_LEFTBRACKET, Key.LBracket);
            MapKey(SDL_SCANCODE_RIGHTBRACKET, Key.RBracket);
            MapKey(SDL_SCANCODE_SEMICOLON, Key.SemiColon);
            MapKey(SDL_SCANCODE_COMMA, Key.Comma);
            MapKey(SDL_SCANCODE_PERIOD, Key.Period);
            MapKey(SDL_SCANCODE_APOSTROPHE, Key.Apostrophe);
            MapKey(SDL_SCANCODE_SLASH, Key.Slash);
            MapKey(SDL_SCANCODE_BACKSLASH, Key.BackSlash);
            MapKey(SDL_SCANCODE_GRAVE, Key.Tilde);
            MapKey(SDL_SCANCODE_EQUALS, Key.Equal);
            MapKey(SDL_SCANCODE_SPACE, Key.Space);
            MapKey(SDL_SCANCODE_RETURN, Key.Return);
            MapKey(SDL_SCANCODE_KP_ENTER, Key.NumpadEnter);
            MapKey(SDL_SCANCODE_BACKSPACE, Key.BackSpace);
            MapKey(SDL_SCANCODE_TAB, Key.Tab);
            MapKey(SDL_SCANCODE_PAGEUP, Key.PageUp);
            MapKey(SDL_SCANCODE_PAGEDOWN, Key.PageDown);
            MapKey(SDL_SCANCODE_END, Key.End);
            MapKey(SDL_SCANCODE_HOME, Key.Home);
            MapKey(SDL_SCANCODE_INSERT, Key.Insert);
            MapKey(SDL_SCANCODE_DELETE, Key.Delete);
            MapKey(SDL_SCANCODE_MINUS, Key.Minus);
            MapKey(SDL_SCANCODE_KP_PLUS, Key.NumpadAdd);
            MapKey(SDL_SCANCODE_KP_MINUS, Key.NumpadSubtract);
            MapKey(SDL_SCANCODE_KP_DIVIDE, Key.NumpadDivide);
            MapKey(SDL_SCANCODE_KP_MULTIPLY, Key.NumpadMultiply);
            MapKey(SDL_SCANCODE_KP_DECIMAL, Key.NumpadDecimal);
            MapKey(SDL_SCANCODE_LEFT, Key.Left);
            MapKey(SDL_SCANCODE_RIGHT, Key.Right);
            MapKey(SDL_SCANCODE_UP, Key.Up);
            MapKey(SDL_SCANCODE_DOWN, Key.Down);
            MapKey(SDL_SCANCODE_F1, Key.F1);
            MapKey(SDL_SCANCODE_F2, Key.F2);
            MapKey(SDL_SCANCODE_F3, Key.F3);
            MapKey(SDL_SCANCODE_F4, Key.F4);
            MapKey(SDL_SCANCODE_F5, Key.F5);
            MapKey(SDL_SCANCODE_F6, Key.F6);
            MapKey(SDL_SCANCODE_F7, Key.F7);
            MapKey(SDL_SCANCODE_F8, Key.F8);
            MapKey(SDL_SCANCODE_F9, Key.F9);
            MapKey(SDL_SCANCODE_F10, Key.F10);
            MapKey(SDL_SCANCODE_F11, Key.F11);
            MapKey(SDL_SCANCODE_F12, Key.F12);
            MapKey(SDL_SCANCODE_F13, Key.F13);
            MapKey(SDL_SCANCODE_F14, Key.F14);
            MapKey(SDL_SCANCODE_F15, Key.F15);
            MapKey(SDL_SCANCODE_PAUSE, Key.Pause);

            KeyMapReverse = new Dictionary<Key, SDL_Scancode>();

            for (var code = 0; code < KeyMap.Length; code++)
            {
                var key = KeyMap[code];
                if (key != Key.Unknown)
                    KeyMapReverse[key] = (SDL_Scancode) code;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void MapKey(SDL_Scancode code, Key key)
            {
                KeyMap[(int)code] = key;
            }
        }
    }
}
