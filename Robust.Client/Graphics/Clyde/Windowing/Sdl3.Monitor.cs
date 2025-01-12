using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDL3;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
    {
        // NOTE: SDL3 calls them "displays". GLFW calls them monitors. GLFW's is the one I'm going with.

        private int _nextMonitorId = 1;

        private readonly Dictionary<int, WinThreadMonitorReg> _winThreadMonitors = new();
        private readonly Dictionary<int, Sdl3MonitorReg> _monitors = new();

        private unsafe void InitMonitors()
        {
            var displayList = (uint*)SDL.SDL_GetDisplays(out var count);
            for (var i = 0; i < count; i++)
            {
                WinThreadSetupMonitor(displayList[i]);
            }

            SDL.SDL_free((nint)displayList);

            // Needed so that monitor creation events get processed.
            ProcessEvents();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void WinThreadSetupMonitor(uint displayId)
        {
            var id = _nextMonitorId++;

            var name = SDL.SDL_GetDisplayName(displayId);
            var modePtr = (SDL.SDL_DisplayMode**)SDL.SDL_GetFullscreenDisplayModes(displayId, out var modeCount);
            var curMode = (SDL.SDL_DisplayMode*)SDL.SDL_GetCurrentDisplayMode(displayId);
            var modes = new VideoMode[modeCount];
            for (var i = 0; i < modes.Length; i++)
            {
                modes[i] = ConvertVideoMode(in *modePtr[i]);
            }

            SDL.SDL_free((nint)modePtr);

            _winThreadMonitors.Add(id, new WinThreadMonitorReg { DisplayId = displayId });

            if (SDL.SDL_GetPrimaryDisplay() == displayId)
                _clyde._primaryMonitorId = id;

            SendEvent(new EventMonitorSetup
            {
                Id = id,
                DisplayId = displayId,
                Name = name,
                AllModes = modes,
                CurrentMode = ConvertVideoMode(in *curMode),
            });
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
            _monitors[ev.Id] = new Sdl3MonitorReg
            {
                DisplayId = ev.DisplayId,
                Handle = impl
            };
        }

        private void WinThreadDestroyMonitor(uint displayId)
        {
            var monitorId = GetMonitorIdFromDisplayId(displayId);
            if (monitorId == 0)
                return;

            _winThreadMonitors.Remove(monitorId);
            SendEvent(new EventMonitorDestroy { Id = monitorId });
        }

        private void ProcessEventDestroyMonitor(EventMonitorDestroy ev)
        {
            _monitors.Remove(ev.Id);
            _clyde._monitorHandles.Remove(ev.Id);
        }

        private int GetMonitorIdFromDisplayId(uint displayId)
        {
            foreach (var (id, monitorReg) in _winThreadMonitors)
            {
                if (monitorReg.DisplayId == displayId)
                {
                    return id;
                }
            }

            return 0;
        }

        private sealed class Sdl3MonitorReg : MonitorReg
        {
            public uint DisplayId;
        }

        private sealed class WinThreadMonitorReg
        {
            public uint DisplayId;
        }
    }
}
