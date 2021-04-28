using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Input;
using Robust.Shared.Maths;

// Makes switching easier.
#if EXCEPTION_TOLERANCE
#define EXCEPTION_TOLERANCE_LOCAL
using System;
using Robust.Shared.Log;
#endif

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        // To avoid re-entrancy bollocks we need ANOTHER queue here to actually dispatch our raw input events.
        // Yes, on top of the two queues inside the GLFW impl.
        // Because the GLFW-impl queue has to get flushed to avoid deadlocks on window creation
        // which is ALSO where key events get raised from in a re-entrant fashion. Yay.
        private readonly Queue<DEventBase> _eventDispatchQueue = new();

        private void DispatchEvents()
        {
            while (_eventDispatchQueue.TryDequeue(out var ev))
            {
#if EXCEPTION_TOLERANCE_LOCAL
                try
#endif
                {
                    DispatchSingleEvent(ev);
                }
#if EXCEPTION_TOLERANCE_LOCAL
                catch (Exception e)
                {
                    Logger.ErrorS("clyde.win", $"Error dispatching window event {ev.GetType().Name}:\n{e}");
                }
#endif
            }
        }

        private void DispatchSingleEvent(DEventBase ev)
        {
            switch (ev)
            {
                case DEventKeyDown keyDown:
                    KeyDown?.Invoke(keyDown.Args);
                    break;
                case DEventKeyUp keyUp:
                    KeyUp?.Invoke(keyUp.Args);
                    break;
                case DEventMouseMove mouseMove:
                    MouseMove?.Invoke(mouseMove.Args);
                    break;
                case DEventScroll scroll:
                    MouseWheel?.Invoke(scroll.Args);
                    break;
                case DEventText text:
                    TextEntered?.Invoke(text.Args);
                    break;
                case DEventWindowClosed winClosed:
                    CloseWindow?.Invoke(winClosed.Args);
                    break;
                case DEventWindowContentScaleChanged winContentScale:
                    OnWindowScaleChanged?.Invoke();
                    break;
                case DEventWindowFocus winFocus:
                    OnWindowFocused?.Invoke(winFocus.Args);
                    break;
                case DEventWindowResized winResized:
                    OnWindowResized?.Invoke(winResized.Args);
                    break;
            }
        }

        private void SendKeyUp(KeyEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventKeyUp(ev));
        }

        private void SendKeyDown(KeyEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventKeyDown(ev));
        }

        private void SendScroll(MouseWheelEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventScroll(ev));
        }

        private void SendCloseWindow(WindowClosedEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowClosed(ev));
        }

        private void SendWindowResized(WindowReg reg, Vector2i oldSize)
        {
            if (reg.IsMainWindow)
            {
                UpdateMainWindowLoadedRtSize();
                GL.Viewport(0, 0, reg.FramebufferSize.X, reg.FramebufferSize.Y);
                CheckGlError();
            }
            else
            {
                reg.RenderTexture!.Dispose();
                CreateWindowRenderTexture(reg);
            }

            var eventArgs = new WindowResizedEventArgs(
                oldSize,
                reg.FramebufferSize,
                reg.Handle);

            _eventDispatchQueue.Enqueue(new DEventWindowResized(eventArgs));
        }

        private void SendWindowContentScaleChanged()
        {
            _eventDispatchQueue.Enqueue(new DEventWindowContentScaleChanged());
        }

        private void SendWindowFocus(WindowFocusedEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowFocus(ev));
        }

        private void SendText(TextEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventText(ev));
        }

        private void SendMouseMove(MouseMoveEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventMouseMove(ev));
        }

        // D stands for Dispatch
        private abstract record DEventBase;

        private sealed record DEventKeyUp(KeyEventArgs Args) : DEventBase;

        private sealed record DEventKeyDown(KeyEventArgs Args) : DEventBase;

        private sealed record DEventScroll(MouseWheelEventArgs Args) : DEventBase;

        private sealed record DEventWindowClosed(WindowClosedEventArgs Args) : DEventBase;

        private sealed record DEventWindowResized(WindowResizedEventArgs Args) : DEventBase;

        private sealed record DEventWindowContentScaleChanged : DEventBase;

        private sealed record DEventWindowFocus(WindowFocusedEventArgs Args) : DEventBase;

        private sealed record DEventText(TextEventArgs Args) : DEventBase;

        private sealed record DEventMouseMove(MouseMoveEventArgs Args) : DEventBase;
    }
}
