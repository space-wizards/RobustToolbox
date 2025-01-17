using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;
using Xilium.CefGlue;
using static Robust.Client.WebView.Cef.CefKeyCodes.ChromiumKeyboardCode;
using static Robust.Client.Input.Keyboard;

namespace Robust.Client.WebView.Cef
{
    internal partial class WebViewManagerCef
    {
        public IWebViewControlImpl MakeControlImpl(WebViewControl owner)
        {
            var shader = _prototypeManager.Index<ShaderPrototype>("bgra");
            var shaderInstance = shader.Instance();
            var impl =  new ControlImpl(owner, shaderInstance);
            _dependencyCollection.InjectDependencies(impl);
            return impl;
        }

        private sealed class ControlImpl : IWebViewControlImpl
        {
            private static readonly Dictionary<Key, CefKeyCodes.ChromiumKeyboardCode> KeyMap = new()
            {
                [Key.A] = VKEY_A,
                [Key.B] = VKEY_B,
                [Key.C] = VKEY_C,
                [Key.D] = VKEY_D,
                [Key.E] = VKEY_E,
                [Key.F] = VKEY_F,
                [Key.G] = VKEY_G,
                [Key.H] = VKEY_H,
                [Key.I] = VKEY_I,
                [Key.J] = VKEY_J,
                [Key.K] = VKEY_K,
                [Key.L] = VKEY_L,
                [Key.M] = VKEY_M,
                [Key.N] = VKEY_N,
                [Key.O] = VKEY_O,
                [Key.P] = VKEY_P,
                [Key.Q] = VKEY_Q,
                [Key.R] = VKEY_R,
                [Key.S] = VKEY_S,
                [Key.T] = VKEY_T,
                [Key.U] = VKEY_U,
                [Key.V] = VKEY_V,
                [Key.W] = VKEY_W,
                [Key.X] = VKEY_X,
                [Key.Y] = VKEY_Y,
                [Key.Z] = VKEY_Z,
                [Key.Num0] = VKEY_0,
                [Key.Num1] = VKEY_1,
                [Key.Num2] = VKEY_2,
                [Key.Num3] = VKEY_3,
                [Key.Num4] = VKEY_4,
                [Key.Num5] = VKEY_5,
                [Key.Num6] = VKEY_6,
                [Key.Num7] = VKEY_7,
                [Key.Num8] = VKEY_8,
                [Key.Num9] = VKEY_9,
                [Key.NumpadNum0] = VKEY_NUMPAD0,
                [Key.NumpadNum1] = VKEY_NUMPAD1,
                [Key.NumpadNum2] = VKEY_NUMPAD2,
                [Key.NumpadNum3] = VKEY_NUMPAD3,
                [Key.NumpadNum4] = VKEY_NUMPAD4,
                [Key.NumpadNum5] = VKEY_NUMPAD5,
                [Key.NumpadNum6] = VKEY_NUMPAD6,
                [Key.NumpadNum7] = VKEY_NUMPAD7,
                [Key.NumpadNum8] = VKEY_NUMPAD8,
                [Key.NumpadNum9] = VKEY_NUMPAD9,
                [Key.Escape] = VKEY_ESCAPE,
                [Key.Control] = VKEY_CONTROL,
                [Key.Shift] = VKEY_SHIFT,
                [Key.Alt] = VKEY_MENU,
                [Key.LSystem] = VKEY_LWIN,
                [Key.RSystem] = VKEY_RWIN,
                [Key.LBracket] = VKEY_OEM_4,
                [Key.RBracket] = VKEY_OEM_6,
                [Key.SemiColon] = VKEY_OEM_1,
                [Key.Comma] = VKEY_OEM_COMMA,
                [Key.Period] = VKEY_OEM_PERIOD,
                [Key.Apostrophe] = VKEY_OEM_7,
                [Key.Slash] = VKEY_OEM_2,
                [Key.BackSlash] = VKEY_OEM_5,
                [Key.Tilde] = VKEY_OEM_3,
                [Key.Equal] = VKEY_OEM_PLUS,
                [Key.Space] = VKEY_SPACE,
                [Key.Return] = VKEY_RETURN,
                [Key.BackSpace] = VKEY_BACK,
                [Key.Tab] = VKEY_TAB,
                [Key.PageUp] = VKEY_PRIOR,
                [Key.PageDown] = VKEY_NEXT,
                [Key.End] = VKEY_END,
                [Key.Home] = VKEY_HOME,
                [Key.Insert] = VKEY_INSERT,
                [Key.Delete] = VKEY_DELETE,
                [Key.Minus] = VKEY_OEM_MINUS,
                [Key.NumpadAdd] = VKEY_ADD,
                [Key.NumpadSubtract] = VKEY_SUBTRACT,
                [Key.NumpadDivide] = VKEY_DIVIDE,
                [Key.NumpadMultiply] = VKEY_MULTIPLY,
                [Key.NumpadDecimal] = VKEY_DECIMAL,
                [Key.Left] = VKEY_LEFT,
                [Key.Right] = VKEY_RIGHT,
                [Key.Up] = VKEY_UP,
                [Key.Down] = VKEY_DOWN,
                [Key.F1] = VKEY_F1,
                [Key.F2] = VKEY_F2,
                [Key.F3] = VKEY_F3,
                [Key.F4] = VKEY_F4,
                [Key.F5] = VKEY_F5,
                [Key.F6] = VKEY_F6,
                [Key.F7] = VKEY_F7,
                [Key.F8] = VKEY_F8,
                [Key.F9] = VKEY_F9,
                [Key.F10] = VKEY_F10,
                [Key.F11] = VKEY_F11,
                [Key.F12] = VKEY_F12,
                [Key.F13] = VKEY_F13,
                [Key.F14] = VKEY_F14,
                [Key.F15] = VKEY_F15,
                [Key.Pause] = VKEY_PAUSE,
            };

            [Dependency] private readonly IClyde _clyde = default!;
            [Dependency] private readonly IInputManager _inputMgr = default!;

            public readonly WebViewControl Owner;
            private readonly ShaderInstance _shaderInstance;

            public ControlImpl(WebViewControl owner, ShaderInstance shaderInstance)
            {
                Owner = owner;
                _shaderInstance = shaderInstance;
            }

            private const int ScrollSpeed = 50;

            private bool _textInputActive;

            private readonly RobustRequestHandler _requestHandler = new(Logger.GetSawmill("root"));
            private LiveData? _data;
            private string _startUrl = "about:blank";

            public string Url
            {
                get => _data == null ? _startUrl : _data.Browser.GetMainFrame().Url;
                set
                {
                    if (_data == null)
                        _startUrl = value;
                    else
                        _data.Browser.GetMainFrame().LoadUrl(value);
                }
            }

            public bool IsLoading => _data?.Browser.IsLoading ?? false;

            public void EnteredTree()
            {
                DebugTools.AssertNull(_data);

                // A funny render handler that will allow us to render to the control.
                var renderer = new ControlRenderHandler(this);

                // A funny web cef client. This can actually be shared by multiple browsers, but I'm not sure how the
                // rendering would work in that case? TODO CEF: Investigate a way to share the web client?
                var client = new RobustCefClient(renderer, _requestHandler, new RobustLoadHandler());

                var info = CefWindowInfo.Create();

                // FUNFACT: If you DO NOT set these below and set info.Width/info.Height instead, you get an external window
                // Good to know, huh? Setup is the same, except you can pass a dummy render handler to the CEF client.
                info.SetAsWindowless(IntPtr.Zero, false); // TODO CEF: Pass parent handle?
                info.WindowlessRenderingEnabled = true;

                var settings = new CefBrowserSettings()
                {
                    WindowlessFrameRate = 60
                };

                // Create the web browser! And by default, we go to about:blank.
                var browser = CefBrowserHost.CreateBrowserSync(info, client, settings, _startUrl);

                var texture = _clyde.CreateBlankTexture<Rgba32>(Vector2i.One);

                _data = new LiveData(texture, client, browser, renderer);
            }

            public void ExitedTree()
            {
                DebugTools.AssertNotNull(_data);

                _data!.Texture.Dispose();
                _data.Browser.GetHost().CloseBrowser(true);
                _data = null;
            }

            public void MouseMove(GUIMouseMoveEventArgs args)
            {
                if (_data == null)
                    return;

                // Logger.Debug();
                var modifiers = CalcMouseModifiers();
                var mouseEvent = new CefMouseEvent(
                    (int)args.RelativePosition.X, (int)args.RelativePosition.Y,
                    modifiers);

                _data.Browser.GetHost().SendMouseMoveEvent(mouseEvent, false);
            }

            public void MouseExited()
            {
                if (_data == null)
                    return;

                var modifiers = CalcMouseModifiers();

                _data.Browser.GetHost().SendMouseMoveEvent(new CefMouseEvent(0, 0, modifiers), true);
            }

            public void MouseWheel(GUIMouseWheelEventArgs args)
            {
                if (_data == null)
                    return;

                var modifiers = CalcMouseModifiers();
                var mouseEvent = new CefMouseEvent(
                    (int)args.RelativePosition.X, (int)args.RelativePosition.Y,
                    modifiers);

                _data.Browser.GetHost().SendMouseWheelEvent(
                    mouseEvent,
                    (int)args.Delta.X * ScrollSpeed,
                    (int)args.Delta.Y * ScrollSpeed);
            }

            public bool RawKeyEvent(in GuiRawKeyEvent guiRawEvent)
            {
                if (_data == null)
                    return false;

                var host = _data.Browser.GetHost();

                if (guiRawEvent.Key is Key.MouseLeft or Key.MouseMiddle or Key.MouseRight)
                {
                    var key = guiRawEvent.Key switch
                    {
                        Key.MouseLeft => CefMouseButtonType.Left,
                        Key.MouseMiddle => CefMouseButtonType.Middle,
                        Key.MouseRight => CefMouseButtonType.Right,
                        _ => default // not possible
                    };

                    var mouseEvent = new CefMouseEvent(
                        guiRawEvent.MouseRelative.X, guiRawEvent.MouseRelative.Y,
                        CefEventFlags.None);

                    // Logger.Debug($"MOUSE: {guiRawEvent.Action} {guiRawEvent.Key} {guiRawEvent.ScanCode} {key}");

                    // TODO: double click support?
                    host.SendMouseClickEvent(mouseEvent, key, guiRawEvent.Action == RawKeyAction.Up, 1);
                }
                else
                {
                    // TODO: Handle left/right modifier keys??
                    if (!KeyMap.TryGetValue(guiRawEvent.Key, out var vkKey))
                        vkKey = default;

                    // Logger.Debug($"{guiRawEvent.Action} {guiRawEvent.Key} {guiRawEvent.ScanCode} {vkKey}");

                    var lParam = 0;
                    lParam |= (guiRawEvent.ScanCode & 0xFF) << 16;
                    if (guiRawEvent.Action != RawKeyAction.Down)
                        lParam |= 1 << 30;

                    if (guiRawEvent.Action == RawKeyAction.Up)
                        lParam |= 1 << 31;

                    var modifiers = CalcModifiers(guiRawEvent.Key);

                    host.SendKeyEvent(new CefKeyEvent
                    {
                        // Repeats are sent as key downs, I guess?
                        EventType = guiRawEvent.Action == RawKeyAction.Up
                            ? CefKeyEventType.KeyUp
                            : CefKeyEventType.RawKeyDown,
                        NativeKeyCode = lParam,
                        // NativeKeyCode = guiRawEvent.ScanCode,
                        WindowsKeyCode = (int)vkKey,
                        IsSystemKey = false, // TODO
                        Modifiers = modifiers
                    });

                    if (guiRawEvent.Action != RawKeyAction.Up && guiRawEvent.Key == Key.Return)
                    {
                        host.SendKeyEvent(new CefKeyEvent
                        {
                            EventType = CefKeyEventType.Char,
                            WindowsKeyCode = '\r',
                            NativeKeyCode = lParam,
                            Modifiers = modifiers
                        });
                    }
                }

                return true;
            }

            private CefEventFlags CalcModifiers(Key key)
            {
                CefEventFlags modifiers = default;

                if (_inputMgr.IsKeyDown(Key.Control))
                    modifiers |= CefEventFlags.ControlDown;

                if (_inputMgr.IsKeyDown(Key.Alt))
                    modifiers |= CefEventFlags.AltDown;

                if (_inputMgr.IsKeyDown(Key.Shift))
                    modifiers |= CefEventFlags.ShiftDown;

                if (_inputMgr.IsKeyDown(Key.Shift))
                    modifiers |= CefEventFlags.ShiftDown;

                return modifiers;
            }

            private CefEventFlags CalcMouseModifiers()
            {
                CefEventFlags modifiers = default;

                if (_inputMgr.IsKeyDown(Key.Control))
                    modifiers |= CefEventFlags.ControlDown;

                if (_inputMgr.IsKeyDown(Key.Alt))
                    modifiers |= CefEventFlags.AltDown;

                if (_inputMgr.IsKeyDown(Key.Shift))
                    modifiers |= CefEventFlags.ShiftDown;

                if (_inputMgr.IsKeyDown(Key.Shift))
                    modifiers |= CefEventFlags.ShiftDown;

                if (_inputMgr.IsKeyDown(Key.MouseLeft))
                    modifiers |= CefEventFlags.LeftMouseButton;

                if (_inputMgr.IsKeyDown(Key.MouseMiddle))
                    modifiers |= CefEventFlags.MiddleMouseButton;

                if (_inputMgr.IsKeyDown(Key.MouseRight))
                    modifiers |= CefEventFlags.RightMouseButton;

                return modifiers;
            }

            public void TextEntered(GUITextEnteredEventArgs args)
            {
                if (_data == null)
                    return;

                var host = _data.Browser.GetHost();

                foreach (var chr in args.Text)
                {
                    host.SendKeyEvent(new CefKeyEvent
                    {
                        EventType = CefKeyEventType.Char,
                        WindowsKeyCode = chr,
                        Character = chr,
                        UnmodifiedCharacter = chr
                    });
                }
            }

            public void Resized()
            {
                if (_data == null)
                    return;

                _data.Browser.GetHost().NotifyMoveOrResizeStarted();
                _data.Browser.GetHost().WasResized();
                _data.Texture.Dispose();
                _data.Texture = _clyde.CreateBlankTexture<Rgba32>((Owner.PixelWidth, Owner.PixelHeight));
            }

            public void Draw(DrawingHandleScreen handle)
            {
                if (_data == null)
                    return;

                var bufImg = _data.Renderer.Buffer.Buffer;

                _data.Texture.SetSubImage(
                    Vector2i.Zero,
                    bufImg,
                    new UIBox2i(
                        0, 0,
                        Math.Min(Owner.PixelWidth, bufImg.Width),
                        Math.Min(Owner.PixelHeight, bufImg.Height)));

                handle.UseShader(_shaderInstance);
                handle.DrawTexture(_data.Texture, Vector2.Zero);
            }

            public void StopLoad()
            {
                if (_data == null)
                    throw new InvalidOperationException();

                _data.Browser.StopLoad();
            }

            public void Reload()
            {
                if (_data == null)
                    throw new InvalidOperationException();

                _data.Browser.Reload();
            }

            public bool GoBack()
            {
                if (_data == null)
                    throw new InvalidOperationException();

                if (!_data.Browser.CanGoBack)
                    return false;

                _data.Browser.GoBack();
                return true;
            }

            public bool GoForward()
            {
                if (_data == null)
                    throw new InvalidOperationException();

                if (!_data.Browser.CanGoForward)
                    return false;

                _data.Browser.GoForward();
                return true;
            }

            public void ExecuteJavaScript(string code)
            {
                if (_data == null)
                    throw new InvalidOperationException();

                // TODO: this should not run until the browser is done loading seriously does this even work?
                _data.Browser.GetMainFrame().ExecuteJavaScript(code, string.Empty, 1);
            }

            public void AddResourceRequestHandler(Action<IRequestHandlerContext> handler)
            {
                _requestHandler.AddResourceRequestHandler(handler);
            }

            public void RemoveResourceRequestHandler(Action<IRequestHandlerContext> handler)
            {
                _requestHandler.RemoveResourceRequestHandler(handler);
            }

            public void AddBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
            {
                _requestHandler.AddBeforeBrowseHandler(handler);
            }

            public void RemoveBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
            {
                _requestHandler.RemoveBeforeBrowseHandler(handler);
            }

            public void FocusEntered()
            {
                if (_textInputActive)
                    Owner.Root?.Window?.TextInputStart();
            }

            public void FocusExited()
            {
                if (_textInputActive)
                    Owner.Root?.Window?.TextInputStop();
            }

            public void TextInputStart()
            {
                _textInputActive = true;
                if (Owner.HasKeyboardFocus())
                    Owner.Root?.Window?.TextInputStart();
            }

            public void TextInputStop()
            {
                _textInputActive = false;
                if (Owner.HasKeyboardFocus())
                    Owner.Root?.Window?.TextInputStop();
            }

            private sealed class LiveData
            {
                public OwnedTexture Texture;
                public readonly RobustCefClient Client;
                public readonly CefBrowser Browser;
                public readonly ControlRenderHandler Renderer;

                public LiveData(
                    OwnedTexture texture,
                    RobustCefClient client,
                    CefBrowser browser,
                    ControlRenderHandler renderer)
                {
                    Texture = texture;
                    Client = client;
                    Browser = browser;
                    Renderer = renderer;
                }
            }
        }

        private sealed class ControlRenderHandler : CefRenderHandler
        {
            public ImageBuffer Buffer { get; }
            private ControlImpl _control;

            internal ControlRenderHandler(ControlImpl control)
            {
                Buffer = new ImageBuffer();
                _control = control;
            }

            protected override CefAccessibilityHandler? GetAccessibilityHandler() => null;

            protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
            {
                if (_control.Owner.Disposed)
                {
                    rect = new CefRectangle();
                    return;
                }

                // TODO CEF: Do we need to pass real screen coords? Cause what we do already works...
                //var screenCoords = _control.ScreenCoordinates;
                //rect = new CefRectangle((int) screenCoords.X, (int) screenCoords.Y, (int)Math.Max(_control.Size.X, 1), (int)Math.Max(_control.Size.Y, 1));

                // We do the max between size and 1 because it will LITERALLY CRASH WITHOUT AN ERROR otherwise.
                rect = new CefRectangle(
                    0, 0,
                    (int)Math.Max(_control.Owner.Size.X, 1), (int)Math.Max(_control.Owner.Size.Y, 1));
            }

            protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
            {
                if (_control.Owner.Disposed)
                    return false;

                screenInfo.DeviceScaleFactor = _control.Owner.UIScale;

                return true;
            }

            protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
            {
                if (_control.Owner.Disposed)
                    return;
            }

            protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects,
                IntPtr buffer, int width, int height)
            {
                if (_control.Owner.Disposed)
                    return;

                foreach (var dirtyRect in dirtyRects)
                {
                    Buffer.UpdateBuffer(width, height, buffer, dirtyRect);
                }
            }

            protected override void OnAcceleratedPaint(
                CefBrowser browser,
                CefPaintElementType type,
                CefRectangle[] dirtyRects,
                in CefAcceleratedPaintInfo info)
            {
                // Unused, but we're forced to implement it so.. NOOP.
            }

            protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
            {
                if (_control.Owner.Disposed)
                    return;
            }

            protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange,
                CefRectangle[] characterBounds)
            {
                if (_control.Owner.Disposed)
                    return;
            }

            protected override void OnVirtualKeyboardRequested(CefBrowser browser, CefTextInputMode inputMode)
            {
                base.OnVirtualKeyboardRequested(browser, inputMode);

                // Treat virtual keyboard requests as a guide for whether we should accept text input.

                if (inputMode == CefTextInputMode.None)
                {
                    _control.TextInputStop();
                }
                else
                {
                    _control.TextInputStart();
                }
            }
        }
    }
}
