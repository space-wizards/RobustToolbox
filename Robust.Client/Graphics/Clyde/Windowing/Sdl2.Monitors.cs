using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDL2;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        // NOTE: SDL2 calls them "displays". GLFW calls them monitors. GLFW's is the one I'm going with.

        // Can't use ClydeHandle because it's not thread safe to allocate.
        private int _nextMonitorId = 1;

        private readonly Dictionary<int, WinThreadMonitorReg> _winThreadMonitors = new();
        private readonly Dictionary<int, Sdl2MonitorReg> _monitors = new();

        private void InitMonitors()
        {
            var numDisplays = SDL.SDL_GetNumVideoDisplays();
            for (var i = 0; i < numDisplays; i++)
            {
                // SDL.SDL_GetDisplayDPI(i, out var ddpi, out var hdpi, out var vdpi);
                // _sawmill.Info($"[{i}] {ddpi} {hdpi} {vdpi}");
                WinThreadSetupMonitor(i);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WinThreadSetupMonitor(int displayIdx)
        {
            var id = _nextMonitorId++;

            var name = SDL.SDL_GetDisplayName(displayIdx);
            var modeCount = SDL.SDL_GetNumDisplayModes(displayIdx);
            SDL.SDL_GetCurrentDisplayMode(displayIdx, out var curMode);
            var modes = new VideoMode[modeCount];
            for (var i = 0; i < modes.Length; i++)
            {
                SDL.SDL_GetDisplayMode(displayIdx, i, out var mode);
                modes[i] = ConvertVideoMode(mode);
            }

            _winThreadMonitors.Add(id, new WinThreadMonitorReg { Id = id, DisplayIdx = displayIdx });

            SendEvent(new EventMonitorSetup(id, name, ConvertVideoMode(curMode), modes));
        }

        private static VideoMode ConvertVideoMode(in SDL.SDL_DisplayMode mode)
        {
            return new()
            {
                Width = (ushort)mode.w,
                Height = (ushort)mode.h,
                RefreshRate = (ushort)mode.refresh_rate,
                // TODO: set bits count based on format (I'm lazy)
                RedBits = 8,
                GreenBits = 8,
                BlueBits = 8,
            };
        }

        private void ProcessSetupMonitor(EventMonitorSetup ev)
        {
            var impl = new MonitorHandle(
                ev.Id,
                ev.Name,
                (ev.CurrentMode.Width, ev.CurrentMode.Height),
                ev.CurrentMode.RefreshRate,
                ev.AllModes);

            _clyde._monitorHandles.Add(ev.Id, impl);
            _monitors[ev.Id] = new Sdl2MonitorReg
            {
                Id = ev.Id,
                Handle = impl
            };
        }

        private void WinThreadDestroyMonitor(int displayIdx)
        {
            var monitorId = 0;

            foreach (var (id, monitorReg) in _winThreadMonitors)
            {
                if (monitorReg.DisplayIdx == displayIdx)
                {
                    monitorId = id;
                    break;
                }
            }

            if (monitorId == 0)
                return;

            // So SDL2 doesn't have a very nice indexing system for monitors like GLFW does.
            // This means that, when a monitor is disconnected, all monitors *after* it get shifted down one slot.
            // Now, this happens *after* the event is fired, to make matters worse.
            // So we're basically trying to match unspecified SDL2 internals here. Great.

            _winThreadMonitors.Remove(monitorId);

            foreach (var (_, reg) in _winThreadMonitors)
            {
                if (reg.DisplayIdx > displayIdx)
                    reg.DisplayIdx -= 1;
            }

            SendEvent(new EventMonitorDestroy(monitorId));
        }

        private void ProcessEventDestroyMonitor(EventMonitorDestroy ev)
        {
            _monitors.Remove(ev.Id);
            _clyde._monitorHandles.Remove(ev.Id);
        }

        private sealed class Sdl2MonitorReg : MonitorReg
        {
            public int Id;
        }

        private sealed class WinThreadMonitorReg
        {
            public int Id;
            public int DisplayIdx;
        }
    }
}
