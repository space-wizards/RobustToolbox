using System;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;

namespace Robust.Client.Graphics.Clyde
{
    partial class Clyde
    {
        private unsafe partial class GlfwWindowingImpl
        {
            public void ProcessEvents()
            {
                // GLFW's callback-based event architecture sucks.
                // And there are ridiculous edge-cases like glfwCreateWindow flushing the event queue (wtf???).
                // So we make our own event buffer and process it manually to work around this madness.
                // This is more similar to how SDL2's event queue works.

                GLFW.PollEvents();

                for (var i = 0; i < _glfwEventQueue.Count; i++)
                {
                    ref var ev = ref _glfwEventQueue[i];

                    try
                    {
                        switch (ev.Type)
                        {
                            case GlfwEventType.MouseButton:
                                ProcessGlfwEventMouseButton(ev.MouseButton);
                                break;
                            case GlfwEventType.CursorPos:
                                ProcessGlfwEventCursorPos(ev.CursorPos);
                                break;
                            case GlfwEventType.Scroll:
                                ProcessGlfwEventScroll(ev.Scroll);
                                break;
                            case GlfwEventType.Key:
                                ProcessGlfwEventKey(ev.Key);
                                break;
                            case GlfwEventType.Char:
                                ProcessGlfwEventChar(ev.Char);
                                break;
                            case GlfwEventType.Monitor:
                                ProcessGlfwEventMonitor(ev.Monitor);
                                break;
                            case GlfwEventType.WindowClose:
                                ProcessGlfwEventWindowClose(ev.WindowClose);
                                break;
                            case GlfwEventType.WindowFocus:
                                ProcessGlfwEventWindowFocus(ev.WindowFocus);
                                break;
                            case GlfwEventType.WindowSize:
                                ProcessGlfwEventWindowSize(ev.WindowSize);
                                break;
                            case GlfwEventType.WindowIconified:
                                ProcessGlfwEventWindowIconify(ev.WindowIconify);
                                break;
                            case GlfwEventType.WindowContentScale:
                                ProcessGlfwEventWindowContentScale(ev.WindowContentScale);
                                break;
                            default:
                                _sawmill.Error("clyde.win", $"Unknown GLFW event type: {ev.Type}");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error(
                            "clyde.win",
                            $"Caught exception in windowing event ({ev.Type}):\n{e}");
                    }
                }

                _glfwEventQueue.Clear();
                if (_glfwEventQueue.Capacity > EventQueueSize)
                {
                    _glfwEventQueue.TrimCapacity(EventQueueSize);
                }
            }

            private void ProcessGlfwEventChar(in GlfwEventChar ev)
            {
                _clyde.SendText(new TextEventArgs(ev.CodePoint));
            }

            private void ProcessGlfwEventCursorPos(in GlfwEventCursorPos ev)
            {
                var windowReg = FindWindow(ev.Window);
                var newPos = ((float) ev.XPos, (float) ev.YPos) * windowReg.PixelRatio;
                var delta = newPos - windowReg.LastMousePos;
                windowReg.LastMousePos = newPos;

                _clyde.MouseMove?.Invoke(new MouseMoveEventArgs(delta, newPos));
            }

            private void ProcessGlfwEventKey(in GlfwEventKey ev)
            {
                EmitKeyEvent(Keyboard.ConvertGlfwKey(ev.Key), ev.Action, ev.Mods);
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

            private void ProcessGlfwEventMouseButton(in GlfwEventMouseButton ev)
            {
                EmitKeyEvent(Mouse.MouseButtonToKey(Mouse.ConvertGlfwButton(ev.Button)), ev.Action, ev.Mods);
            }

            private void ProcessGlfwEventScroll(in GlfwEventScroll ev)
            {
                var windowReg = FindWindow(ev.Window);
                var eventArgs = new MouseWheelEventArgs(
                    ((float) ev.XOffset, (float) ev.YOffset),
                    windowReg.LastMousePos);
                _clyde.SendScroll(eventArgs);
            }

            private void ProcessGlfwEventWindowClose(in GlfwEventWindowClose ev)
            {
                var windowReg = FindWindow(ev.Window);
                _clyde.SendCloseWindow(new WindowClosedEventArgs(windowReg.Handle));
            }

            private void ProcessGlfwEventWindowSize(in GlfwEventWindowSize ev)
            {
                var window = ev.Window;
                var width = ev.Width;
                var height = ev.Height;

                var windowReg = FindWindow(window);
                var oldSize = windowReg.FramebufferSize;
                GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
                windowReg.FramebufferSize = (fbW, fbH);
                windowReg.WindowSize = (width, height);

                if (fbW == 0 || fbH == 0 || width == 0 || height == 0)
                    return;

                windowReg.PixelRatio = windowReg.FramebufferSize / windowReg.WindowSize;

                _clyde.SendWindowResized(windowReg, oldSize);
            }

            private void ProcessGlfwEventWindowContentScale(in GlfwEventWindowContentScale ev)
            {
                var windowReg = FindWindow(ev.Window);
                windowReg.WindowScale = (ev.XScale, ev.YScale);
                _clyde.SendWindowContentScaleChanged();
            }

            private void ProcessGlfwEventWindowIconify(in GlfwEventWindowIconify ev)
            {
                var windowReg = FindWindow(ev.Window);
                windowReg.IsMinimized = ev.Iconified;
            }

            private void ProcessGlfwEventWindowFocus(in GlfwEventWindowFocus ev)
            {
                var windowReg = FindWindow(ev.Window);
                windowReg.IsFocused = ev.Focused;
                _clyde.SendWindowFocus(new WindowFocusedEventArgs(ev.Focused, windowReg.Handle));
            }
        }
    }
}
