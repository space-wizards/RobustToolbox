using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDL3;
using Key = Robust.Client.Input.Keyboard.Key;
using Button = Robust.Client.Input.Mouse.Button;
using SC = SDL3.SDL.SDL_Scancode;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
    {
        // Indices are values of SDL_Scancode
        private static readonly Key[] KeyMap;
        private static readonly FrozenDictionary<Key, SC> KeyMapReverse;
        private static readonly Button[] MouseButtonMap;

        // TODO: to avoid having to ask the windowing thread, key names are cached.
        private readonly Dictionary<Key, string> _printableKeyNameMap = new();

        private void ReloadKeyMap()
        {
            // This may be ran concurrently from the windowing thread.
            lock (_printableKeyNameMap)
            {
                _printableKeyNameMap.Clear();

                // TODO: Validate this is correct in SDL3.

                // List of mappable keys from SDL2's source appears to be:
                // entries in SDL_default_keymap that aren't an SDLK_ enum reference.
                // (the actual logic is more nuanced, but it appears to match the above)
                // Comes out to these two ranges:

                for (var k = SC.SDL_SCANCODE_A; k <= SC.SDL_SCANCODE_0; k++)
                {
                    CacheKey(k);
                }

                for (var k = SC.SDL_SCANCODE_MINUS; k <= SC.SDL_SCANCODE_SLASH; k++)
                {
                    CacheKey(k);
                }

                void CacheKey(SC scancode)
                {
                    var rKey = ConvertSdl3Scancode(scancode);
                    if (rKey == Key.Unknown)
                        return;

                    // TODO: SDL_GetKeyFromScancode correct?
                    var name = SDL.SDL_GetKeyName(
                        SDL.SDL_GetKeyFromScancode(scancode, SDL.SDL_Keymod.SDL_KMOD_NONE, false));

                    if (!string.IsNullOrEmpty(name))
                        _printableKeyNameMap.Add(rKey, name);
                }
            }
        }

        public string? KeyGetName(Key key)
        {
            lock (_printableKeyNameMap)
            {
                if (_printableKeyNameMap.TryGetValue(key, out var name))
                    return name;

                return null;
            }
        }

        internal static Key ConvertSdl3Scancode(SC scancode)
        {
            return KeyMap[(int) scancode];
        }

        public static Button ConvertSdl3Button(int button)
        {
            return MouseButtonMap[button];
        }

        static Sdl3WindowingImpl()
        {
            MouseButtonMap = new Button[6];
            MouseButtonMap[SDL.SDL_BUTTON_LEFT] = Button.Left;
            MouseButtonMap[SDL.SDL_BUTTON_RIGHT] = Button.Right;
            MouseButtonMap[SDL.SDL_BUTTON_MIDDLE] = Button.Middle;
            MouseButtonMap[SDL.SDL_BUTTON_X1] = Button.Button4;
            MouseButtonMap[SDL.SDL_BUTTON_X2] = Button.Button5;

            KeyMap = new Key[(int) SC.SDL_SCANCODE_COUNT];
            MapKey(SC.SDL_SCANCODE_A, Key.A);
            MapKey(SC.SDL_SCANCODE_B, Key.B);
            MapKey(SC.SDL_SCANCODE_C, Key.C);
            MapKey(SC.SDL_SCANCODE_D, Key.D);
            MapKey(SC.SDL_SCANCODE_E, Key.E);
            MapKey(SC.SDL_SCANCODE_F, Key.F);
            MapKey(SC.SDL_SCANCODE_G, Key.G);
            MapKey(SC.SDL_SCANCODE_H, Key.H);
            MapKey(SC.SDL_SCANCODE_I, Key.I);
            MapKey(SC.SDL_SCANCODE_J, Key.J);
            MapKey(SC.SDL_SCANCODE_K, Key.K);
            MapKey(SC.SDL_SCANCODE_L, Key.L);
            MapKey(SC.SDL_SCANCODE_M, Key.M);
            MapKey(SC.SDL_SCANCODE_N, Key.N);
            MapKey(SC.SDL_SCANCODE_O, Key.O);
            MapKey(SC.SDL_SCANCODE_P, Key.P);
            MapKey(SC.SDL_SCANCODE_Q, Key.Q);
            MapKey(SC.SDL_SCANCODE_R, Key.R);
            MapKey(SC.SDL_SCANCODE_S, Key.S);
            MapKey(SC.SDL_SCANCODE_T, Key.T);
            MapKey(SC.SDL_SCANCODE_U, Key.U);
            MapKey(SC.SDL_SCANCODE_V, Key.V);
            MapKey(SC.SDL_SCANCODE_W, Key.W);
            MapKey(SC.SDL_SCANCODE_X, Key.X);
            MapKey(SC.SDL_SCANCODE_Y, Key.Y);
            MapKey(SC.SDL_SCANCODE_Z, Key.Z);
            MapKey(SC.SDL_SCANCODE_0, Key.Num0);
            MapKey(SC.SDL_SCANCODE_1, Key.Num1);
            MapKey(SC.SDL_SCANCODE_2, Key.Num2);
            MapKey(SC.SDL_SCANCODE_3, Key.Num3);
            MapKey(SC.SDL_SCANCODE_4, Key.Num4);
            MapKey(SC.SDL_SCANCODE_5, Key.Num5);
            MapKey(SC.SDL_SCANCODE_6, Key.Num6);
            MapKey(SC.SDL_SCANCODE_7, Key.Num7);
            MapKey(SC.SDL_SCANCODE_8, Key.Num8);
            MapKey(SC.SDL_SCANCODE_9, Key.Num9);
            MapKey(SC.SDL_SCANCODE_KP_0, Key.NumpadNum0);
            MapKey(SC.SDL_SCANCODE_KP_1, Key.NumpadNum1);
            MapKey(SC.SDL_SCANCODE_KP_2, Key.NumpadNum2);
            MapKey(SC.SDL_SCANCODE_KP_3, Key.NumpadNum3);
            MapKey(SC.SDL_SCANCODE_KP_4, Key.NumpadNum4);
            MapKey(SC.SDL_SCANCODE_KP_5, Key.NumpadNum5);
            MapKey(SC.SDL_SCANCODE_KP_6, Key.NumpadNum6);
            MapKey(SC.SDL_SCANCODE_KP_7, Key.NumpadNum7);
            MapKey(SC.SDL_SCANCODE_KP_8, Key.NumpadNum8);
            MapKey(SC.SDL_SCANCODE_KP_9, Key.NumpadNum9);
            MapKey(SC.SDL_SCANCODE_ESCAPE, Key.Escape);
            MapKey(SC.SDL_SCANCODE_LCTRL, Key.Control);
            MapKey(SC.SDL_SCANCODE_RCTRL, Key.Control);
            MapKey(SC.SDL_SCANCODE_RSHIFT, Key.Shift);
            MapKey(SC.SDL_SCANCODE_LSHIFT, Key.Shift);
            MapKey(SC.SDL_SCANCODE_LALT, Key.Alt);
            MapKey(SC.SDL_SCANCODE_RALT, Key.Alt);
            MapKey(SC.SDL_SCANCODE_LGUI, Key.LSystem);
            MapKey(SC.SDL_SCANCODE_RGUI, Key.RSystem);
            MapKey(SC.SDL_SCANCODE_MENU, Key.Menu);
            MapKey(SC.SDL_SCANCODE_LEFTBRACKET, Key.LBracket);
            MapKey(SC.SDL_SCANCODE_RIGHTBRACKET, Key.RBracket);
            MapKey(SC.SDL_SCANCODE_SEMICOLON, Key.SemiColon);
            MapKey(SC.SDL_SCANCODE_COMMA, Key.Comma);
            MapKey(SC.SDL_SCANCODE_PERIOD, Key.Period);
            MapKey(SC.SDL_SCANCODE_APOSTROPHE, Key.Apostrophe);
            MapKey(SC.SDL_SCANCODE_SLASH, Key.Slash);
            MapKey(SC.SDL_SCANCODE_BACKSLASH, Key.BackSlash);
            MapKey(SC.SDL_SCANCODE_GRAVE, Key.Tilde);
            MapKey(SC.SDL_SCANCODE_EQUALS, Key.Equal);
            MapKey(SC.SDL_SCANCODE_SPACE, Key.Space);
            MapKey(SC.SDL_SCANCODE_RETURN, Key.Return);
            MapKey(SC.SDL_SCANCODE_KP_ENTER, Key.NumpadEnter);
            MapKey(SC.SDL_SCANCODE_BACKSPACE, Key.BackSpace);
            MapKey(SC.SDL_SCANCODE_TAB, Key.Tab);
            MapKey(SC.SDL_SCANCODE_PAGEUP, Key.PageUp);
            MapKey(SC.SDL_SCANCODE_PAGEDOWN, Key.PageDown);
            MapKey(SC.SDL_SCANCODE_END, Key.End);
            MapKey(SC.SDL_SCANCODE_HOME, Key.Home);
            MapKey(SC.SDL_SCANCODE_INSERT, Key.Insert);
            MapKey(SC.SDL_SCANCODE_DELETE, Key.Delete);
            MapKey(SC.SDL_SCANCODE_MINUS, Key.Minus);
            MapKey(SC.SDL_SCANCODE_KP_PLUS, Key.NumpadAdd);
            MapKey(SC.SDL_SCANCODE_KP_MINUS, Key.NumpadSubtract);
            MapKey(SC.SDL_SCANCODE_KP_DIVIDE, Key.NumpadDivide);
            MapKey(SC.SDL_SCANCODE_KP_MULTIPLY, Key.NumpadMultiply);
            MapKey(SC.SDL_SCANCODE_KP_DECIMAL, Key.NumpadDecimal);
            MapKey(SC.SDL_SCANCODE_LEFT, Key.Left);
            MapKey(SC.SDL_SCANCODE_RIGHT, Key.Right);
            MapKey(SC.SDL_SCANCODE_UP, Key.Up);
            MapKey(SC.SDL_SCANCODE_DOWN, Key.Down);
            MapKey(SC.SDL_SCANCODE_F1, Key.F1);
            MapKey(SC.SDL_SCANCODE_F2, Key.F2);
            MapKey(SC.SDL_SCANCODE_F3, Key.F3);
            MapKey(SC.SDL_SCANCODE_F4, Key.F4);
            MapKey(SC.SDL_SCANCODE_F5, Key.F5);
            MapKey(SC.SDL_SCANCODE_F6, Key.F6);
            MapKey(SC.SDL_SCANCODE_F7, Key.F7);
            MapKey(SC.SDL_SCANCODE_F8, Key.F8);
            MapKey(SC.SDL_SCANCODE_F9, Key.F9);
            MapKey(SC.SDL_SCANCODE_F10, Key.F10);
            MapKey(SC.SDL_SCANCODE_F11, Key.F11);
            MapKey(SC.SDL_SCANCODE_F12, Key.F12);
            MapKey(SC.SDL_SCANCODE_F13, Key.F13);
            MapKey(SC.SDL_SCANCODE_F14, Key.F14);
            MapKey(SC.SDL_SCANCODE_F15, Key.F15);
            MapKey(SC.SDL_SCANCODE_F16, Key.F16);
            MapKey(SC.SDL_SCANCODE_F17, Key.F17);
            MapKey(SC.SDL_SCANCODE_F18, Key.F18);
            MapKey(SC.SDL_SCANCODE_F19, Key.F19);
            MapKey(SC.SDL_SCANCODE_F20, Key.F20);
            MapKey(SC.SDL_SCANCODE_F21, Key.F21);
            MapKey(SC.SDL_SCANCODE_F22, Key.F22);
            MapKey(SC.SDL_SCANCODE_F23, Key.F23);
            MapKey(SC.SDL_SCANCODE_F24, Key.F24);
            MapKey(SC.SDL_SCANCODE_PAUSE, Key.Pause);

            var keyMapReverse = new Dictionary<Key, SC>();

            for (var code = 0; code < KeyMap.Length; code++)
            {
                var key = KeyMap[code];
                if (key != Key.Unknown)
                    keyMapReverse[key] = (SC) code;
            }

            KeyMapReverse = keyMapReverse.ToFrozenDictionary();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void MapKey(SC code, Key key)
            {
                KeyMap[(int)code] = key;
            }
        }
    }
}
