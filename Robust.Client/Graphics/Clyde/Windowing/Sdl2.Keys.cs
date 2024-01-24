using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Robust.Client.Input;
using Robust.Shared;
using SDL;
using static SDL.SDL;
using Key = Robust.Client.Input.Keyboard.Key;
using Button = Robust.Client.Input.Mouse.Button;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class SdlWindowingImpl
    {
        // Indices are values of SDL_Scancode
        private static readonly Key[] KeyMap;
        private static readonly FrozenDictionary<Key, SDL_Scancode> KeyMapReverse;
        private static readonly Button[] MouseButtonMap;

        // TODO: to avoid having to ask the windowing thread, key names are cached.
        private readonly Dictionary<Key, string> _printableKeyNameMap = new();

        private void ReloadKeyMap()
        {
            // This may be ran concurrently from the windowing thread.
            lock (_printableKeyNameMap)
            {
                _printableKeyNameMap.Clear();

                // List of mappable keys from SDL2's source appears to be:
                // entries in SDL_default_keymap that aren't an SDLK_ enum reference.
                // (the actual logic is more nuanced, but it appears to match the above)
                // Comes out to these two ranges:

                for (var k = SDL_Scancode.A; k <= SDL_Scancode._0; k++)
                {
                    CacheKey(k);
                }

                for (var k = SDL_Scancode.Minus; k <= SDL_Scancode.Slash; k++)
                {
                    CacheKey(k);
                }

                void CacheKey(SDL_Scancode scancode)
                {
                    var rKey = ConvertSdl2Scancode(scancode);
                    if (rKey == Key.Unknown)
                        return;

                    var name = SDL_GetKeyNameString(SDL_GetKeyFromScancode(scancode));

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

        internal static Key ConvertSdl2Scancode(SDL_Scancode scancode)
        {
            return KeyMap[(int) scancode];
        }

        public static Button ConvertSdl2Button(int button)
        {
            return MouseButtonMap[button];
        }

        static SdlWindowingImpl()
        {
            MouseButtonMap = new Button[6];
            MouseButtonMap[SDL_BUTTON_LEFT] = Button.Left;
            MouseButtonMap[SDL_BUTTON_RIGHT] = Button.Right;
            MouseButtonMap[SDL_BUTTON_MIDDLE] = Button.Middle;
            MouseButtonMap[SDL_BUTTON_X1] = Button.Button4;
            MouseButtonMap[SDL_BUTTON_X2] = Button.Button5;

            KeyMap = new Key[512]; // since sdl2.0
            MapKey(SDL_Scancode.A, Key.A);
            MapKey(SDL_Scancode.B, Key.B);
            MapKey(SDL_Scancode.C, Key.C);
            MapKey(SDL_Scancode.D, Key.D);
            MapKey(SDL_Scancode.E, Key.E);
            MapKey(SDL_Scancode.F, Key.F);
            MapKey(SDL_Scancode.G, Key.G);
            MapKey(SDL_Scancode.H, Key.H);
            MapKey(SDL_Scancode.I, Key.I);
            MapKey(SDL_Scancode.J, Key.J);
            MapKey(SDL_Scancode.K, Key.K);
            MapKey(SDL_Scancode.L, Key.L);
            MapKey(SDL_Scancode.M, Key.M);
            MapKey(SDL_Scancode.N, Key.N);
            MapKey(SDL_Scancode.O, Key.O);
            MapKey(SDL_Scancode.P, Key.P);
            MapKey(SDL_Scancode.Q, Key.Q);
            MapKey(SDL_Scancode.R, Key.R);
            MapKey(SDL_Scancode.S, Key.S);
            MapKey(SDL_Scancode.T, Key.T);
            MapKey(SDL_Scancode.U, Key.U);
            MapKey(SDL_Scancode.V, Key.V);
            MapKey(SDL_Scancode.W, Key.W);
            MapKey(SDL_Scancode.X, Key.X);
            MapKey(SDL_Scancode.Y, Key.Y);
            MapKey(SDL_Scancode.Z, Key.Z);
            MapKey(SDL_Scancode._0, Key.Num0);
            MapKey(SDL_Scancode._1, Key.Num1);
            MapKey(SDL_Scancode._2, Key.Num2);
            MapKey(SDL_Scancode._3, Key.Num3);
            MapKey(SDL_Scancode._4, Key.Num4);
            MapKey(SDL_Scancode._5, Key.Num5);
            MapKey(SDL_Scancode._6, Key.Num6);
            MapKey(SDL_Scancode._7, Key.Num7);
            MapKey(SDL_Scancode._8, Key.Num8);
            MapKey(SDL_Scancode._9, Key.Num9);
            MapKey(SDL_Scancode.Escape, Key.Escape);
            MapKey(SDL_Scancode.LeftControl, Key.Control);
            MapKey(SDL_Scancode.RightControl, Key.Control);
            MapKey(SDL_Scancode.RightShirt, Key.Shift);
            MapKey(SDL_Scancode.LeftShirt, Key.Shift);
            MapKey(SDL_Scancode.LeftAlt, Key.Alt);
            MapKey(SDL_Scancode.RightAlt, Key.Alt);
            MapKey(SDL_Scancode.LeftGui, Key.LSystem);
            MapKey(SDL_Scancode.RightGui, Key.RSystem);
            MapKey(SDL_Scancode.Menu, Key.Menu);
            MapKey(SDL_Scancode.LeftBracket, Key.LBracket);
            MapKey(SDL_Scancode.RightBracket, Key.RBracket);
            MapKey(SDL_Scancode.Semicolon, Key.SemiColon);
            MapKey(SDL_Scancode.Comma, Key.Comma);
            MapKey(SDL_Scancode.Period, Key.Period);
            MapKey(SDL_Scancode.Apostrophe, Key.Apostrophe);
            MapKey(SDL_Scancode.Slash, Key.Slash);
            MapKey(SDL_Scancode.Backslash, Key.BackSlash);
            MapKey(SDL_Scancode.Grave, Key.Tilde);
            MapKey(SDL_Scancode.Equals, Key.Equal);
            MapKey(SDL_Scancode.Space, Key.Space);
            MapKey(SDL_Scancode.Return, Key.Return);
            MapKey(SDL_Scancode.Backspace, Key.BackSpace);
            MapKey(SDL_Scancode.Tab, Key.Tab);
            MapKey(SDL_Scancode.PageUp, Key.PageUp);
            MapKey(SDL_Scancode.PageDown, Key.PageDown);
            MapKey(SDL_Scancode.End, Key.End);
            MapKey(SDL_Scancode.Home, Key.Home);
            MapKey(SDL_Scancode.Insert, Key.Insert);
            MapKey(SDL_Scancode.Delete, Key.Delete);
            MapKey(SDL_Scancode.Minus, Key.Minus);
            MapKey(SDL_Scancode.Left, Key.Left);
            MapKey(SDL_Scancode.Right, Key.Right);
            MapKey(SDL_Scancode.Up, Key.Up);
            MapKey(SDL_Scancode.Down, Key.Down);
            MapKey(SDL_Scancode.F1, Key.F1);
            MapKey(SDL_Scancode.F2, Key.F2);
            MapKey(SDL_Scancode.F3, Key.F3);
            MapKey(SDL_Scancode.F4, Key.F4);
            MapKey(SDL_Scancode.F5, Key.F5);
            MapKey(SDL_Scancode.F6, Key.F6);
            MapKey(SDL_Scancode.F7, Key.F7);
            MapKey(SDL_Scancode.F8, Key.F8);
            MapKey(SDL_Scancode.F9, Key.F9);
            MapKey(SDL_Scancode.F10, Key.F10);
            MapKey(SDL_Scancode.F11, Key.F11);
            MapKey(SDL_Scancode.F12, Key.F12);
            MapKey(SDL_Scancode.F13, Key.F13);
            MapKey(SDL_Scancode.F14, Key.F14);
            MapKey(SDL_Scancode.F15, Key.F15);
            MapKey(SDL_Scancode.F16, Key.F16);
            MapKey(SDL_Scancode.F17, Key.F17);
            MapKey(SDL_Scancode.F18, Key.F18);
            MapKey(SDL_Scancode.F19, Key.F19);
            MapKey(SDL_Scancode.F20, Key.F20);
            MapKey(SDL_Scancode.F21, Key.F21);
            MapKey(SDL_Scancode.F22, Key.F22);
            MapKey(SDL_Scancode.F23, Key.F23);
            MapKey(SDL_Scancode.F24, Key.F24);
            MapKey(SDL_Scancode.Pause, Key.Pause);
            MapKey(SDL_Scancode.Kp0, Key.NumpadNum0);
            MapKey(SDL_Scancode.Kp1, Key.NumpadNum1);
            MapKey(SDL_Scancode.Kp2, Key.NumpadNum2);
            MapKey(SDL_Scancode.Kp3, Key.NumpadNum3);
            MapKey(SDL_Scancode.Kp4, Key.NumpadNum4);
            MapKey(SDL_Scancode.Kp5, Key.NumpadNum5);
            MapKey(SDL_Scancode.Kp6, Key.NumpadNum6);
            MapKey(SDL_Scancode.Kp7, Key.NumpadNum7);
            MapKey(SDL_Scancode.Kp8, Key.NumpadNum8);
            MapKey(SDL_Scancode.Kp9, Key.NumpadNum9);
            MapKey(SDL_Scancode.KpEnter, Key.NumpadEnter);
            MapKey(SDL_Scancode.KpPlus, Key.NumpadAdd);
            MapKey(SDL_Scancode.KpMinus, Key.NumpadSubtract);
            MapKey(SDL_Scancode.KpDivide, Key.NumpadDivide);
            MapKey(SDL_Scancode.KpMultiply, Key.NumpadMultiply);
            MapKey(SDL_Scancode.KpDecimal, Key.NumpadDecimal);

            var keyMapReverse = new Dictionary<Key, SDL_Scancode>();

            for (var code = 0; code < KeyMap.Length; code++)
            {
                var key = KeyMap[code];
                if (key != Key.Unknown)
                    keyMapReverse[key] = (SDL_Scancode) code;
            }

            KeyMapReverse = keyMapReverse.ToFrozenDictionary();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void MapKey(SDL_Scancode code, Key key)
            {
                KeyMap[(int)code] = key;
            }
        }
    }
}
