using System;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;

namespace Robust.Client.Graphics.Clyde
{
    partial class Clyde
    {
        private partial class GlfwWindowingImpl
        {
            public void ProcessEvents()
            {
                while (_eventReader.TryRead(out var ev))
                {
                    try
                    {
                        ProcessEvent(ev);
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error(
                            "clyde.win",
                            $"Caught exception in windowing event ({ev.GetType()}):\n{e}");
                    }
                }
            }

            // Block waiting on the windowing -> game thread channel.
            // I swear to god do not use this unless you know what you are doing.
            private void WaitEvents()
            {
                _eventReader.WaitToReadAsync().AsTask().Wait();
            }

            private void ProcessEvent(EventBase ev)
            {
                switch (ev)
                {
                    case EventMouseButton mb:
                        ProcessEventMouseButton(mb);
                        break;
                    case EventCursorPos cp:
                        ProcessEventCursorPos(cp);
                        break;
                    case EventScroll s:
                        ProcessEventScroll(s);
                        break;
                    case EventKey k:
                        ProcessEventKey(k);
                        break;
                    case EventChar c:
                        ProcessEventChar(c);
                        break;
                    case EventMonitorSetup ms:
                        ProcessSetupMonitor(ms);
                        break;
                    case EventMonitorDestroy md:
                        ProcessEventDestroyMonitor(md);
                        break;
                    case EventWindowCreate wCreate:
                        FinishWindowCreate(wCreate);
                        break;
                    case EventWindowClose wc:
                        ProcessEventWindowClose(wc);
                        break;
                    case EventWindowFocus wf:
                        ProcessEventWindowFocus(wf);
                        break;
                    case EventWindowSize ws:
                        ProcessEventWindowSize(ws);
                        break;
                    case EventWindowPos wp:
                        ProcessEventWindowPos(wp);
                        break;
                    case EventWindowIconify wi:
                        ProcessEventWindowIconify(wi);
                        break;
                    case EventWindowContentScale cs:
                        ProcessEventWindowContentScale(cs);
                        break;
                    default:
                        _sawmill.Error("clyde.win", $"Unknown GLFW event type: {ev.GetType()}");
                        break;
                }
            }

            private void ProcessEventChar(EventChar ev)
            {
                _clyde.SendText(new TextEventArgs(ev.CodePoint));
            }

            private void ProcessEventCursorPos(EventCursorPos ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                var newPos = ((float) ev.XPos, (float) ev.YPos) * windowReg.PixelRatio;
                var delta = newPos - windowReg.LastMousePos;
                windowReg.LastMousePos = newPos;

                _clyde.MouseMove?.Invoke(new MouseMoveEventArgs(delta, newPos));
            }

            private void ProcessEventKey(EventKey ev)
            {
                EmitKeyEvent(ConvertGlfwKey(ev.Key), ev.Action, ev.Mods);
            }

            private void EmitKeyEvent(Keyboard.Key key, InputAction action, KeyModifiers mods)
            {
                var shift = (mods & KeyModifiers.Shift) != 0;
                var alt = (mods & KeyModifiers.Alt) != 0;
                var control = (mods & KeyModifiers.Control) != 0;
                var system = (mods & KeyModifiers.Super) != 0;

                var ev = new KeyEventArgs(
                    key,
                    action == InputAction.Repeat,
                    alt, control, shift, system);

                switch (action)
                {
                    case InputAction.Release:
                        _clyde.SendKeyUp(ev);
                        break;
                    case InputAction.Press:
                    case InputAction.Repeat:
                        _clyde.SendKeyDown(ev);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            }

            private void ProcessEventMouseButton(EventMouseButton ev)
            {
                EmitKeyEvent(Mouse.MouseButtonToKey(ConvertGlfwButton(ev.Button)), ev.Action, ev.Mods);
            }

            private void ProcessEventScroll(EventScroll ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                var eventArgs = new MouseWheelEventArgs(
                    ((float) ev.XOffset, (float) ev.YOffset),
                    windowReg.LastMousePos);
                _clyde.SendScroll(eventArgs);
            }

            private void ProcessEventWindowClose(EventWindowClose ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                _clyde.SendCloseWindow(new WindowClosedEventArgs(windowReg.Handle));
            }

            private void ProcessEventWindowSize(EventWindowSize ev)
            {
                var window = ev.Window;
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

                windowReg.PixelRatio = windowReg.FramebufferSize / windowReg.WindowSize;

                _clyde.SendWindowResized(windowReg, oldSize);
            }

            private void ProcessEventWindowPos(EventWindowPos ev)
            {
                var window = ev.Window;
                var x = ev.X;
                var y = ev.Y;

                var windowReg = FindWindow(window);
                if (windowReg == null)
                    return;

                windowReg.WindowPos = (x, y);
            }

            private void ProcessEventWindowContentScale(EventWindowContentScale ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                windowReg.WindowScale = (ev.XScale, ev.YScale);
                _clyde.SendWindowContentScaleChanged();
            }

            private void ProcessEventWindowIconify(EventWindowIconify ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                windowReg.IsMinimized = ev.Iconified;
            }

            private void ProcessEventWindowFocus(EventWindowFocus ev)
            {
                var windowReg = FindWindow(ev.Window);
                if (windowReg == null)
                    return;

                windowReg.IsFocused = ev.Focused;
                _clyde.SendWindowFocus(new WindowFocusedEventArgs(ev.Focused, windowReg.Handle));
            }
        }
    }
}
