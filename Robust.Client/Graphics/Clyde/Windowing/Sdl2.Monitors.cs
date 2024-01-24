using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDL;
using static SDL.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class SdlWindowingImpl
    {
        // NOTE: SDL2 calls them "displays". GLFW calls them monitors. GLFW's is the one I'm going with.

        // Can't use ClydeHandle because it's not thread safe to allocate.
        private int _nextMonitorId = 1;

        private readonly Dictionary<int, WinThreadMonitorReg> _winThreadMonitors = new();
        private readonly Dictionary<int, Sdl2MonitorReg> _monitors = new();

        private void InitMonitors()
        {
            var numDisplays = SDL_GetDisplays();
            foreach (var displayId in numDisplays)
            {
                // SDL.SDL_GetDisplayDPI(i, out var ddpi, out var hdpi, out var vdpi);
                // _sawmill.Info($"[{i}] {ddpi} {hdpi} {vdpi}");
                WinThreadSetupMonitor(displayId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void WinThreadSetupMonitor(SDL_DisplayID displayIdx)
        {
            var id = _nextMonitorId++;

            var name = SDL_GetDisplayNameString(displayIdx);
            var displayModes = SDL_GetFullscreenDisplayModes(displayIdx, out var modeCount);
            var curMode = SDL_GetCurrentDisplayMode(displayIdx);

            var modes = new VideoMode[modeCount];
            for (var i = 0; i < modes.Length; i++)
            {
                modes[i] = ConvertVideoMode(displayModes[i]);
            }

            _winThreadMonitors.Add(id, new WinThreadMonitorReg { Id = id, DisplayId = displayIdx });

            SendEvent(new EventMonitorSetup(id, name, ConvertVideoMode(curMode), modes));

            if (displayIdx == 0)
                _clyde._primaryMonitorId = id;
        }

        private static unsafe VideoMode ConvertVideoMode(in SDL_DisplayMode* mode)
        {
            return new VideoMode
            {
                Width = (ushort)mode->w,
                Height = (ushort)mode->h,
                RefreshRate = (ushort)mode->refresh_rate,
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

        private void WinThreadDestroyMonitor(SDL_DisplayID displayIdx)
        {
            var monitorId = 0;

            foreach (var (id, monitorReg) in _winThreadMonitors)
            {
                if (monitorReg.DisplayId == displayIdx)
                {
                    monitorId = id;
                    break;
                }
            }

            if (monitorId == 0)
                return;

            _winThreadMonitors.Remove(monitorId);
            // TODO check if sdl2 bug still exist; new function just returns a list of SDL_DisplayID

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
            public SDL_DisplayID DisplayId;
        }
    }
}
