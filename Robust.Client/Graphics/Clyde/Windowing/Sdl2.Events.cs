using System;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using TerraFX.Interop.Windows;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;
using static SDL2.SDL.SDL_Keymod;
using static SDL2.SDL.SDL_WindowEventID;
using Key = Robust.Client.Input.Keyboard.Key;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        public void ProcessEvents(bool single = false)
        {
            while (_eventReader.TryRead(out var ev))
            {
                try
                {
                    ProcessEvent(ev);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception in windowing event ({ev.GetType().Name}):\n{e}");
                }

                if (single)
                    break;
            }
        }

        // Block waiting on the windowing -> game thread channel.
        // I swear to god do not use this unless you know what you are doing.
        private void WaitEvents()
        {
            _eventReader.WaitToReadAsync().AsTask().Wait();
        }

        private void ProcessEvent(EventBase evb)
        {
            switch (evb)
            {
                case EventWindowCreate wCreate:
                    FinishWindowCreate(wCreate);
                    break;
                case EventWindow ev:
                    ProcessEventWindow(ev);
                    break;
                case EventKey ev:
                    ProcessEventKey(ev);
                    break;
                case EventWindowSize ev:
                    ProcessEventWindowSize(ev);
                    break;
                case EventText ev:
                    ProcessEventText(ev);
                    break;
                case EventTextEditing ev:
                    ProcessEventTextEditing(ev);
                    break;
                case EventMouseMotion ev:
                    ProcessEventMouseMotion(ev);
                    break;
                case EventMouseButton ev:
                    ProcessEventMouseButton(ev);
                    break;
                case EventWheel ev:
                    ProcessEventWheel(ev);
                    break;
                case EventMonitorSetup ev:
                    ProcessSetupMonitor(ev);
                    break;
                case EventWindowsFakeV ev:
                    ProcessWindowsFakeV(ev);
                    break;
                default:
                    _sawmill.Error($"Unknown SDL2 event type: {evb.GetType().Name}");
                    break;
            }
        }

        private void ProcessEventWindow(EventWindow ev)
        {
            var window = FindWindow(ev.WindowId);
            if (window == null)
                return;

            switch (ev.EventId)
            {
                case SDL_WINDOWEVENT_CLOSE:
                    _clyde.SendCloseWindow(window, new WindowRequestClosedEventArgs(window.Handle));
                    break;
                case SDL_WINDOWEVENT_ENTER:
                    _clyde._currentHoveredWindow = window;
                    _clyde.SendMouseEnterLeave(new MouseEnterLeaveEventArgs(window.Handle, true));
                    break;
                case SDL_WINDOWEVENT_LEAVE:
                    if (_clyde._currentHoveredWindow == window)
                        _clyde._currentHoveredWindow = null;

                    _clyde.SendMouseEnterLeave(new MouseEnterLeaveEventArgs(window.Handle, false));
                    break;
                case SDL_WINDOWEVENT_MINIMIZED:
                    window.IsMinimized = true;
                    break;
                case SDL_WINDOWEVENT_RESTORED:
                    window.IsMinimized = false;
                    break;
                case SDL_WINDOWEVENT_FOCUS_GAINED:
                    window.IsFocused = true;
                    _clyde.SendWindowFocus(new WindowFocusedEventArgs(true, window.Handle));
                    break;
                case SDL_WINDOWEVENT_FOCUS_LOST:
                    window.IsFocused = false;
                    _clyde.SendWindowFocus(new WindowFocusedEventArgs(false, window.Handle));
                    break;
            }
        }

        private void ProcessEventWheel(EventWheel ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var eventArgs = new MouseWheelEventArgs(
                (ev.XOffset, ev.YOffset),
                new ScreenCoordinates(windowReg.LastMousePos, windowReg.Id));

            _clyde.SendScroll(eventArgs);
        }

        private void ProcessEventMouseButton(EventMouseButton ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var mods = SDL_GetModState();
            var button = ConvertSdl2Button(ev.Button);
            var key = Mouse.MouseButtonToKey(button);
            EmitKeyEvent(key, ev.Type, false, mods, 0);
        }

        private void ProcessEventMouseMotion(EventMouseMotion ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var newPos = (ev.X, ev.Y) * windowReg.PixelRatio;
            // SDL2 does give us delta positions, but I'm worried about rounding errors thanks to DPI stuff.
            var delta = newPos - windowReg.LastMousePos;
            windowReg.LastMousePos = newPos;

            _clyde._currentHoveredWindow = windowReg;

            _clyde.SendMouseMove(new MouseMoveEventArgs(delta, new ScreenCoordinates(newPos, windowReg.Id)));
        }

        private void ProcessEventText(EventText ev)
        {
            _clyde.SendText(new TextEnteredEventArgs(ev.Text));
        }

        private void ProcessEventTextEditing(EventTextEditing ev)
        {
            _clyde.SendTextEditing(new TextEditingEventArgs(ev.Text, ev.Start, ev.Length));
        }

        private void ProcessEventWindowSize(EventWindowSize ev)
        {
            var window = ev.WindowId;
            var width = ev.Width;
            var height = ev.Height;
            var fbW = ev.FramebufferWidth;
            var fbH = ev.FramebufferHeight;

            var windowReg = FindWindow(window);
            if (windowReg == null)
                return;

            var oldSize = windowReg.FramebufferSize;
            windowReg.FramebufferSize = (fbW, fbH);
            windowReg.WindowSize = (width, height);

            if (fbW == 0 || fbH == 0 || width == 0 || height == 0)
                return;

            windowReg.PixelRatio = windowReg.FramebufferSize / (Vector2)windowReg.WindowSize;

            if (windowReg.WindowScale != (ev.XScale, ev.YScale))
            {
                windowReg.WindowScale = (ev.XScale, ev.YScale);
                _clyde.SendWindowContentScaleChanged(new WindowContentScaleEventArgs(windowReg.Handle));
            }

            _clyde.SendWindowResized(windowReg, oldSize);
        }

        private void ProcessEventKey(EventKey ev)
        {
            EmitKeyEvent(ConvertSdl2Scancode(ev.Scancode), ev.Type, ev.Repeat, ev.Mods, ev.Scancode);
        }

        private void EmitKeyEvent(Key key, SDL_EventType type, bool repeat, SDL_Keymod mods, SDL_Scancode scancode)
        {
            var shift = (mods & KMOD_SHIFT) != 0;
            var alt = (mods & KMOD_ALT) != 0;
            var control = (mods & KMOD_CTRL) != 0;
            var system = (mods & KMOD_GUI) != 0;

            var ev = new KeyEventArgs(
                key,
                repeat,
                alt, control, shift, system,
                (int)scancode);

            switch (type)
            {
                case SDL_KEYUP:
                case SDL_MOUSEBUTTONUP:
                    _clyde.SendKeyUp(ev);
                    break;
                case SDL_KEYDOWN:
                case SDL_MOUSEBUTTONDOWN:
                    _clyde.SendKeyDown(ev);
                    break;
            }
        }

        private void ProcessWindowsFakeV(EventWindowsFakeV ev)
        {
            var type = (int)ev.Message switch
            {
                WM.WM_KEYUP => SDL_KEYUP,
                WM.WM_KEYDOWN => SDL_KEYDOWN,
                _ => throw new ArgumentOutOfRangeException()
            };

            var key = (int)ev.WParam switch
            {
                0x56 /* V */ => Key.V,
                VK.VK_CONTROL => Key.Control,
                _ => throw new ArgumentOutOfRangeException()
            };

            EmitKeyEvent(key, type, false, 0, 0);
        }
    }
}
