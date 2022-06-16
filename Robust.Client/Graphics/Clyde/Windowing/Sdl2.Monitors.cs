using System.Collections.Generic;
using static SDL2.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        // NOTE: SDL2 calls them "displays". GLFW calls them monitors. GLFW's is the one I'm going with.

        // private readonly Dictionary<int, WinThreadMonitorReg> _winThreadMonitors = new();

        private void InitMonitors()
        {
            var numDisplays = SDL_GetNumVideoDisplays();
            for (var i = 0; i < numDisplays; i++)
            {
                SDL_GetDisplayDPI(i, out var ddpi, out var hdpi, out var vdpi);
                _sawmill.Info($"[{i}] {ddpi} {hdpi} {vdpi}");
            }
        }

    }
}
