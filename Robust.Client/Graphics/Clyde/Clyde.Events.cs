// Makes switching easier.
#if EXCEPTION_TOLERANCE
#define EXCEPTION_TOLERANCE_LOCAL
using Robust.Shared.Log;
#endif

using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Input;
using Robust.Shared;
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
        private bool _mainWindowSizeDirty;
        private TimeSpan? _mainWindowSettingsSaveAt;
        private static readonly TimeSpan MainWindowSettingsSaveDelay = TimeSpan.FromSeconds(1);

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
                    _sawmillWin.Error($"Error dispatching window event {ev.GetType().Name}:\n{e}");
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
                case DEventTextEditing(var args):
                    TextEditing?.Invoke(args);
                    break;
                case DEventWindowClosed(var reg, var args):
                    if (reg.IsMainWindow)
                        SaveMainWindowSettings(reg);

                    reg.RequestClosed?.Invoke(args);
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
                case DEventWindowResized(var reg, var args):
                    UpdateMainWindowSizeCVar(reg);
                    reg.Resized?.Invoke(args);
                    OnWindowResized?.Invoke(args);
                    break;
                case DEventInputModeChanged:
                    RaiseInputModeChanged();
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

        private void SendCloseWindow(WindowReg windowReg, WindowRequestClosedEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowClosed(windowReg, ev));
        }

        private void SendWindowResized(WindowReg reg, Vector2i oldSize)
        {
            if (!reg.IsVisible) // Only send this for open windows
                return;

            var loaded = RtToLoaded(reg.RenderTarget);
            loaded.Size = reg.FramebufferSize;

            _glContext!.WindowResized(reg, oldSize);

            var eventArgs = new WindowResizedEventArgs(
                oldSize,
                reg.FramebufferSize,
                reg.Handle);

            _eventDispatchQueue.Enqueue(new DEventWindowResized(reg, eventArgs));
        }

        private void UpdateMainWindowSizeCVar(WindowReg reg)
        {
            if (!reg.IsMainWindow || _windowMode != WindowMode.Windowed || reg.IsMaximized)
                return;

            var size = reg.WindowSize;
            if (size.X <= 0 || size.Y <= 0)
                return;

            if (_cfg.GetCVar(CVars.DisplayWidth) == size.X &&
                _cfg.GetCVar(CVars.DisplayHeight) == size.Y)
            {
                return;
            }

            _cfg.SetCVar(CVars.DisplayWidth, size.X);
            _cfg.SetCVar(CVars.DisplayHeight, size.Y);
            DirtyMainWindowSettings();
        }

        private void SaveMainWindowSize()
        {
            if (!_mainWindowSizeDirty)
                return;

            _cfg.SaveToFile();
            _mainWindowSizeDirty = false;
            _mainWindowSettingsSaveAt = null;
        }

        private void SaveMainWindowSettings(WindowReg reg)
        {
            UpdateMainWindowSizeCVar(reg);
            UpdateMainWindowPositionCVar(reg);
            UpdateMainWindowMaximizedCVar(reg, reg.IsMaximized);
            SaveMainWindowSize();
        }

        private void UpdateMainWindowMaximizedCVar(WindowReg reg, bool maximized)
        {
            if (!reg.IsMainWindow || _windowMode != WindowMode.Windowed)
                return;

            UpdateMainWindowMonitorCVar(reg);

            if (_cfg.GetCVar(CVars.DisplayWindowMaximized) == maximized)
                return;

            _cfg.SetCVar(CVars.DisplayWindowMaximized, maximized);
            DirtyMainWindowSettings();
        }

        private void UpdateMainWindowPositionCVar(WindowReg reg)
        {
            if (!reg.IsMainWindow || _windowMode != WindowMode.Windowed || reg.IsMaximized)
                return;

            var position = reg.WindowPos;
            var monitor = GetMonitorForWindow(reg);
            if (monitor == null)
                return;

            var monitorString = GetMonitorConfigString(monitor);
            if (_cfg.GetCVar(CVars.DisplayWindowPosX) == position.X &&
                _cfg.GetCVar(CVars.DisplayWindowPosY) == position.Y &&
                _cfg.GetCVar(CVars.DisplayWindowMonitor) == monitorString)
            {
                return;
            }

            _cfg.SetCVar(CVars.DisplayWindowPosX, position.X);
            _cfg.SetCVar(CVars.DisplayWindowPosY, position.Y);
            _cfg.SetCVar(CVars.DisplayWindowMonitor, monitorString);
            DirtyMainWindowSettings();
        }

        private void UpdateMainWindowMonitorCVar(WindowReg reg)
        {
            if (!reg.IsMainWindow || _windowMode != WindowMode.Windowed)
                return;

            var monitor = GetMonitorForWindow(reg);
            if (monitor == null)
                return;

            var monitorString = GetMonitorConfigString(monitor);
            if (_cfg.GetCVar(CVars.DisplayWindowMonitor) == monitorString)
                return;

            _cfg.SetCVar(CVars.DisplayWindowMonitor, monitorString);
            DirtyMainWindowSettings();
        }

        private void DirtyMainWindowSettings()
        {
            _mainWindowSizeDirty = true;
            _mainWindowSettingsSaveAt = _gameTiming.RealTime + MainWindowSettingsSaveDelay;
        }

        // This just exists so we can save settings if rider is closed / game crashes and we don't get a clean shutdown.
        private void SaveMainWindowSettingsIfDue()
        {
            if (!_mainWindowSizeDirty ||
                _mainWindowSettingsSaveAt is not { } saveAt ||
                _gameTiming.RealTime < saveAt)
            {
                return;
            }

            SaveMainWindowSize();
        }

        private void SendWindowContentScaleChanged(WindowContentScaleEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowContentScaleChanged(ev));
        }

        private void SendWindowFocus(WindowFocusedEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventWindowFocus(ev));
        }

        private void SendText(TextEnteredEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventText(ev));
        }

        private void SendTextEditing(TextEditingEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventTextEditing(ev));
        }

        private void SendMouseMove(MouseMoveEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventMouseMove(ev));
        }

        private void SendMouseEnterLeave(MouseEnterLeaveEventArgs ev)
        {
            _eventDispatchQueue.Enqueue(new DEventMouseEnterLeave(ev));
        }

        private void SendInputModeChanged()
        {
            _eventDispatchQueue.Enqueue(new DEventInputModeChanged());
        }

        // D stands for Dispatch
        private abstract record DEventBase;

        private sealed record DEventKeyUp(KeyEventArgs Args) : DEventBase;

        private sealed record DEventKeyDown(KeyEventArgs Args) : DEventBase;

        private sealed record DEventScroll(MouseWheelEventArgs Args) : DEventBase;

        private sealed record DEventWindowClosed(WindowReg Reg, WindowRequestClosedEventArgs Args) : DEventBase;

        private sealed record DEventWindowResized(WindowReg Reg, WindowResizedEventArgs Args) : DEventBase;

        private sealed record DEventWindowContentScaleChanged(WindowContentScaleEventArgs Args) : DEventBase;

        private sealed record DEventWindowFocus(WindowFocusedEventArgs Args) : DEventBase;

        private sealed record DEventText(TextEnteredEventArgs Args) : DEventBase;
        private sealed record DEventTextEditing(TextEditingEventArgs Args) : DEventBase;

        private sealed record DEventMouseMove(MouseMoveEventArgs Args) : DEventBase;
        private sealed record DEventMouseEnterLeave(MouseEnterLeaveEventArgs Args) : DEventBase;
        private sealed record DEventInputModeChanged : DEventBase;
    }
}
