// Makes switching easier.
#if EXCEPTION_TOLERANCE
#define EXCEPTION_TOLERANCE_LOCAL
using System;
using Robust.Shared.Log;
#endif

using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Input;
using Robust.Shared.Maths;

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
                case DEventKeyDown(var args):
                    KeyDown?.Invoke(args);
                    break;
                case DEventKeyUp(var args):
                    KeyUp?.Invoke(args);
                    break;
                case DEventMouseMove(var args):
                    MouseMove?.Invoke(args);
                    break;
                case DEventMouseEnterLeave(var args):
                    MouseEnterLeave?.Invoke(args);
                    break;
                case DEventScroll(var args):
                    MouseWheel?.Invoke(args);
                    break;
                case DEventText(var args):
                    TextEntered?.Invoke(args);
                    break;
                case DEventWindowClosed(var reg, var args):
                    reg.Closed?.Invoke(args);
                    CloseWindow?.Invoke(args);

                    if (reg.DisposeOnClose && !reg.IsMainWindow)
                        DoDestroyWindow(reg);
                    break;
                case DEventWindowContentScaleChanged(var args):
                    OnWindowScaleChanged?.Invoke(args);
                    break;
                case DEventWindowFocus(var args):
                    OnWindowFocused?.Invoke(args);
                    break;
                case DEventWindowResized(var args):
                    OnWindowResized?.Invoke(args);
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

        private void SendCloseWindow(WindowReg windowReg, WindowClosedEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowClosed(windowReg, ev));
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

        private void SendWindowContentScaleChanged(WindowContentScaleEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowContentScaleChanged(ev));
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

        private void SendMouseEnterLeave(MouseEnterLeaveEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventMouseEnterLeave(ev));
        }

        // D stands for Dispatch
        private abstract record DEventBase;

        private sealed record DEventKeyUp(KeyEventArgs Args) : DEventBase;

        private sealed record DEventKeyDown(KeyEventArgs Args) : DEventBase;

        private sealed record DEventScroll(MouseWheelEventArgs Args) : DEventBase;

        private sealed record DEventWindowClosed(WindowReg Reg, WindowClosedEventArgs Args) : DEventBase;

        private sealed record DEventWindowResized(WindowResizedEventArgs Args) : DEventBase;

        private sealed record DEventWindowContentScaleChanged(WindowContentScaleEventArgs Args) : DEventBase;

        private sealed record DEventWindowFocus(WindowFocusedEventArgs Args) : DEventBase;

        private sealed record DEventText(TextEventArgs Args) : DEventBase;

        private sealed record DEventMouseMove(MouseMoveEventArgs Args) : DEventBase;
        private sealed record DEventMouseEnterLeave(MouseEnterLeaveEventArgs Args) : DEventBase;
    }
}
