using System;
using System.Numerics;
using Robust.Client.Input;
using Robust.Shared.Map;
using SDL3;
using Key = Robust.Client.Input.Keyboard.Key;
using ET = SDL3.SDL.SDL_EventType;
using SDL_Keymod = SDL3.SDL.SDL_Keymod;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
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
                case EventWindowMisc ev:
                    ProcessEventWindowMisc(ev);
                    break;
                case EventKey ev:
                    ProcessEventKey(ev);
                    break;
                case EventWindowPixelSize ev:
                    ProcessEventWindowSize(ev);
                    break;
                case EventWindowContentScale ev:
                    ProcessEventWindowContentScale(ev);
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
                case EventKeyMapChanged:
                    ProcessKeyMapChanged();
                    break;
                case EventQuit:
                    ProcessEventQuit();
                    break;
                default:
                    _sawmill.Error($"Unknown SDL3 event type: {evb.GetType().Name}");
                    break;
            }
        }

        private void ProcessEventQuit()
        {
            // Interpret quit as closing of the main window.
            var window = _clyde._mainWindow!;
            _clyde.SendCloseWindow(window, new WindowRequestClosedEventArgs(window.Handle));
        }

        private void ProcessEventWindowMisc(EventWindowMisc ev)
        {
            var window = FindWindow(ev.WindowId);
            if (window == null)
                return;

            switch (ev.EventId)
            {
                case ET.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                    _clyde.SendCloseWindow(window, new WindowRequestClosedEventArgs(window.Handle));
                    break;
                case ET.SDL_EVENT_WINDOW_MOUSE_ENTER:
                    _clyde._currentHoveredWindow = window;
                    _clyde.SendMouseEnterLeave(new MouseEnterLeaveEventArgs(window.Handle, true));
                    break;
                case ET.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    if (_clyde._currentHoveredWindow == window)
                        _clyde._currentHoveredWindow = null;

                    _clyde.SendMouseEnterLeave(new MouseEnterLeaveEventArgs(window.Handle, false));
                    break;
                case ET.SDL_EVENT_WINDOW_MINIMIZED:
                    window.IsMinimized = true;
                    break;
                case ET.SDL_EVENT_WINDOW_RESTORED:
                    window.IsMinimized = false;
                    break;
                case ET.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    window.IsFocused = true;
                    _clyde.SendWindowFocus(new WindowFocusedEventArgs(true, window.Handle));
                    break;
                case ET.SDL_EVENT_WINDOW_FOCUS_LOST:
                    window.IsFocused = false;
                    _clyde.SendWindowFocus(new WindowFocusedEventArgs(false, window.Handle));
                    break;
                case ET.SDL_EVENT_WINDOW_MOVED:
                    window.WindowPos = (ev.Data1, ev.Data2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessEventWheel(EventWheel ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var eventArgs = new MouseWheelEventArgs(
                new Vector2(ev.XOffset, ev.YOffset),
                new ScreenCoordinates(windowReg.LastMousePos, windowReg.Id));

            _clyde.SendScroll(eventArgs);
        }

        private void ProcessEventMouseButton(EventMouseButton ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var button = ConvertSdl3Button(ev.Button);
            var key = Mouse.MouseButtonToKey(button);
            EmitKeyEvent(key, ev.Type, false, ev.Mods, 0);
        }

        private void ProcessEventMouseMotion(EventMouseMotion ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            var newPos = new Vector2(ev.X, ev.Y) * windowReg.PixelRatio;
            var delta = new Vector2(ev.XRel, ev.YRel);
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

        private void ProcessEventWindowSize(EventWindowPixelSize ev)
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

            _clyde.SendWindowResized(windowReg, oldSize);
        }

        private void ProcessEventWindowContentScale(EventWindowContentScale ev)
        {
            var windowReg = FindWindow(ev.WindowId);
            if (windowReg == null)
                return;

            windowReg.WindowScale = new Vector2(ev.Scale, ev.Scale);
            _clyde.SendWindowContentScaleChanged(new WindowContentScaleEventArgs(windowReg.Handle));
        }

        private void ProcessEventKey(EventKey ev)
        {
            EmitKeyEvent(ConvertSdl3Scancode(ev.Scancode), ev.Type, ev.Repeat, ev.Mods, ev.Scancode);
        }

        private void EmitKeyEvent(Key key, ET type, bool repeat, SDL.SDL_Keymod mods, SDL.SDL_Scancode scancode)
        {
            var shift = (mods & SDL_Keymod.SDL_KMOD_SHIFT) != 0;
            var alt = (mods & SDL_Keymod.SDL_KMOD_ALT) != 0;
            var control = (mods & SDL_Keymod.SDL_KMOD_CTRL) != 0;
            var system = (mods & SDL_Keymod.SDL_KMOD_GUI) != 0;

            var ev = new KeyEventArgs(
                key,
                repeat,
                alt, control, shift, system,
                (int)scancode);

            switch (type)
            {
                case ET.SDL_EVENT_KEY_UP:
                case ET.SDL_EVENT_MOUSE_BUTTON_UP:
                    _clyde.SendKeyUp(ev);
                    break;
                case ET.SDL_EVENT_KEY_DOWN:
                case ET.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    _clyde.SendKeyDown(ev);
                    break;
            }
        }

        private void ProcessKeyMapChanged()
        {
            _clyde.SendInputModeChanged();
        }
    }
}
