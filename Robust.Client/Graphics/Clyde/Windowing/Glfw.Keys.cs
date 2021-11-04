using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared;
using System.Threading;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;
using Robust.Shared.Localization;
using GlfwKey = OpenToolkit.GraphicsLibraryFramework.Keys;
using GlfwButton = OpenToolkit.GraphicsLibraryFramework.MouseButton;
using static Robust.Client.Input.Mouse;
using static Robust.Client.Input.Keyboard;
using Robust.Shared.IoC;
using Robust.Shared.Configuration;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed partial class GlfwWindowingImpl
        {
            // TODO: to avoid having to ask the windowing thread, key names are cached.
            // This means they don't update correctly if the user switches keyboard mode. RIP.

            private readonly Dictionary<Key, string> _printableKeyNameMap = new();

            private void InitKeyMap()
            {
                _printableKeyNameMap.Clear();
                // From GLFW's source code: this is the actual list of "printable" keys
                // that GetKeyName returns something for.
                CacheKey(Keys.KeyPadEqual);
                for (var k = Keys.KeyPad0; k <= Keys.KeyPadAdd; k++)
                {
                    CacheKey(k);
                }

                for (var k = Keys.Apostrophe; k <= Keys.World2; k++)
                {
                    CacheKey(k);
                }

                void CacheKey(GlfwKey key)
                {
                    var rKey = ConvertGlfwKey(key);
                    if (rKey == Key.Unknown)
                        return;

                    string name;

                    if (!_clyde._cfg.GetCVar(CVars.DisplayUSQWERTYHotkeys))
                    {
                        name = GLFW.GetKeyName(key, 0);
                    }
                    else
                    {
                        name = key.ToString();
                    }

                    if (!string.IsNullOrEmpty(name))
                        _printableKeyNameMap.Add(rKey, name);
                }
            }

            public string KeyGetName(Keyboard.Key key)
            {
                if (_printableKeyNameMap.TryGetValue(key, out var name))
                {
                    var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
                    return textInfo.ToTitleCase(name);
                }

                name = Keyboard.GetSpecialKeyName(key, _loc);
                if (name != null)
                {
                    return Loc.GetString(name);
                }

                return Loc.GetString("<unknown key>");
            }

            public static Button ConvertGlfwButton(GlfwButton button)
            {
                return MouseButtonMap[button];
            }

            private static readonly Dictionary<GlfwButton, Button> MouseButtonMap = new()
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

            private static readonly Dictionary<GlfwKey, Key> KeyMap;
            private static readonly Dictionary<Key, GlfwKey> KeyMapReverse;


            internal static Key ConvertGlfwKey(GlfwKey key)
            {
                if (KeyMap.TryGetValue(key, out var result))
                {
                    return result;
                }

                return Key.Unknown;
            }

            internal static GlfwKey ConvertGlfwKeyReverse(Key key)
            {
                if (KeyMapReverse.TryGetValue(key, out var result))
                {
                    return result;
                }

                return GlfwKey.Unknown;
            }

            static GlfwWindowingImpl()
            {
                KeyMap = new Dictionary<GlfwKey, Key>
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

                KeyMapReverse = new Dictionary<Key, GlfwKey>();

                foreach (var (key, value) in KeyMap)
                {
                    KeyMapReverse[value] = key;
                }
            }
        }
    }
}
