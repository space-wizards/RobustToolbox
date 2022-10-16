using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;
using static SDL2.SDL.SDL_WindowEventID;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        [UnmanagedCallersOnly(CallConvs = new []{typeof(CallConvCdecl)})]
        private static unsafe int EventWatch(void* userdata, SDL_Event* sdlevent)
        {
            var obj = (Sdl2WindowingImpl) GCHandle.FromIntPtr((IntPtr)userdata).Target!;
            ref readonly var ev = ref *sdlevent;

            obj.ProcessSdl2Event(in ev);

            return 0;
        }

        private void ProcessSdl2Event(in SDL_Event ev)
        {
            switch (ev.type)
            {
                case SDL_WINDOWEVENT:
                    ProcessSdl2EventWindow(ev.window);
                    break;
                case SDL_KEYDOWN:
                case SDL_KEYUP:
                    ProcessSdl2KeyEvent(ev.key);
                    break;
                case SDL_TEXTINPUT:
                    ProcessSdl2EventTextInput(ev.text);
                    break;
                case SDL_MOUSEMOTION:
                    ProcessSdl2EventMouseMotion(ev.motion);
                    break;
                case SDL_MOUSEBUTTONDOWN:
                case SDL_MOUSEBUTTONUP:
                    ProcessSdl2EventMouseButton(ev.button);
                    break;
                case SDL_MOUSEWHEEL:
                    ProcessSdl2EventMouseWheel(ev.wheel);
                    break;
                case SDL_DISPLAYEVENT:
                    ProcessSdl2EventDisplay(ev.display);
                    break;
            }
        }

        private void ProcessSdl2EventDisplay(SDL_DisplayEvent evDisplay)
        {
            switch (evDisplay.displayEvent)
            {
                case SDL_DisplayEventID.SDL_DISPLAYEVENT_CONNECTED:
                    WinThreadSetupMonitor((int) evDisplay.display);
                    break;
                case SDL_DisplayEventID.SDL_DISPLAYEVENT_DISCONNECTED:
                    WinThreadDestroyMonitor((int) evDisplay.display);
                    break;
            }
        }

        private void ProcessSdl2EventMouseWheel(in SDL_MouseWheelEvent ev)
        {
            SendEvent(new EventWheel(ev.windowID, ev.preciseX, ev.preciseY));
        }

        private void ProcessSdl2EventMouseButton(in SDL_MouseButtonEvent ev)
        {
            SendEvent(new EventMouseButton(ev.windowID, ev.type, ev.button));
        }

        private void ProcessSdl2EventMouseMotion(in SDL_MouseMotionEvent ev)
        {
            // _sawmill.Info($"{evMotion.x}, {evMotion.y}, {evMotion.xrel}, {evMotion.yrel}");
            SendEvent(new EventMouseMotion(ev.windowID, ev.x, ev.y, ev.xrel, ev.yrel));
        }

        private unsafe void ProcessSdl2EventTextInput(in SDL_TextInputEvent ev)
        {
            fixed (byte* text = ev.text)
            {
                var str = Marshal.PtrToStringUTF8((IntPtr)text) ?? "";
                SendEvent(new EventText(ev.windowID, str));
            }
        }

        private void ProcessSdl2KeyEvent(in SDL_KeyboardEvent ev)
        {
            SendEvent(new EventKey(
                ev.windowID,
                ev.keysym.scancode,
                ev.type,
                ev.repeat != 0,
                ev.keysym.mod));
        }

        private void ProcessSdl2EventWindow(in SDL_WindowEvent ev)
        {
            var window = SDL_GetWindowFromID(ev.windowID);

            switch (ev.windowEvent)
            {
                case SDL_WINDOWEVENT_SIZE_CHANGED:
                    var width = ev.data1;
                    var height = ev.data2;
                    SDL_GL_GetDrawableSize(window, out var fbW, out var fbH);
                    var (xScale, yScale) = GetWindowScale(window);

                    _sawmill.Debug($"{width}x{height}, {fbW}x{fbH}, {xScale}x{yScale}");

                    SendEvent(new EventWindowSize(ev.windowID, width, height, fbW, fbH, xScale, yScale));
                    break;

                default:
                    SendEvent(new EventWindow(ev.windowID, ev.windowEvent));
                    break;
            }
        }

        private abstract record EventBase;

        private record EventWindowCreate(
            Sdl2WindowCreateResult Result,
            TaskCompletionSource<Sdl2WindowCreateResult> Tcs
        ) : EventBase;

        private record EventKey(
            uint WindowId,
            SDL_Scancode Scancode,
            SDL_EventType Type,
            bool Repeat,
            SDL_Keymod Mods
        ) : EventBase;

        private record EventMouseMotion(
            uint WindowId,
            int X, int Y,
            int XRel, int YRel
        ) : EventBase;

        private record EventMouseButton(
            uint WindowId,
            SDL_EventType Type,
            byte Button
        ) : EventBase;

        private record EventText(
            uint WindowId,
            string Text
        ) : EventBase;

        private record EventWindowSize(
            uint WindowId,
            int Width,
            int Height,
            int FramebufferWidth,
            int FramebufferHeight,
            float XScale,
            float YScale
        ) : EventBase;

        private record EventWheel(
            uint WindowId,
            float XOffset,
            float YOffset
        ) : EventBase;

        // SDL_WindowEvents that don't have special handling like size.
        private record EventWindow(
            uint WindowId,
            SDL_WindowEventID EventId
        ) : EventBase;

        private record EventMonitorSetup
        (
            int Id,
            string Name,
            VideoMode CurrentMode,
            VideoMode[] AllModes
        ) : EventBase;

        private record EventMonitorDestroy
        (
            int Id
        ) : EventBase;
    }
}
