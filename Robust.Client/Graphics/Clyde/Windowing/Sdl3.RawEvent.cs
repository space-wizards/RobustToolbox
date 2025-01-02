using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static SDL3.SDL;
using static SDL3.SDL.SDL_EventType;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
    {
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe byte EventWatch(void* userdata, SDL_Event* sdlevent)
        {
            var obj = (Sdl3WindowingImpl)GCHandle.FromIntPtr((IntPtr)userdata).Target!;

            obj.ProcessSdl3Event(in *sdlevent);

            return 0;
        }

        private void ProcessSdl3Event(in SDL_Event ev)
        {
            switch ((SDL_EventType)ev.type)
            {
                case SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                    ProcessSdl3EventWindowPixelSizeChanged(in ev.window);
                    break;
                case SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                    ProcessSdl3EventWindowDisplayScaleChanged(in ev.window);
                    break;
                case SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                case SDL_EVENT_WINDOW_MOUSE_ENTER:
                case SDL_EVENT_WINDOW_MOUSE_LEAVE:
                case SDL_EVENT_WINDOW_MINIMIZED:
                case SDL_EVENT_WINDOW_RESTORED:
                case SDL_EVENT_WINDOW_FOCUS_GAINED:
                case SDL_EVENT_WINDOW_FOCUS_LOST:
                    ProcessSdl3EventWindowMisc(in ev.window);
                    break;
                case SDL_EVENT_KEY_DOWN:
                case SDL_EVENT_KEY_UP:
                    ProcessSdl3KeyEvent(in ev.key);
                    break;
                case SDL_EVENT_TEXT_INPUT:
                    ProcessSdl3EventTextInput(in ev.text);
                    break;
                case SDL_EVENT_TEXT_EDITING:
                    ProcessSdl3EventTextEditing(in ev.edit);
                    break;
                case SDL_EVENT_KEYMAP_CHANGED:
                    ProcessSdl3EventKeyMapChanged();
                    break;
                case SDL_EVENT_MOUSE_MOTION:
                    ProcessSdl3EventMouseMotion(in ev.motion);
                    break;
                case SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EVENT_MOUSE_BUTTON_UP:
                    ProcessSdl3EventMouseButton(in ev.button);
                    break;
                case SDL_EVENT_MOUSE_WHEEL:
                    ProcessSdl3EventMouseWheel(in ev.wheel);
                    break;
                case SDL_EVENT_DISPLAY_ADDED:
                    WinThreadSetupMonitor(ev.display.displayID);
                    break;
                case SDL_EVENT_DISPLAY_REMOVED:
                    WinThreadDestroyMonitor(ev.display.displayID);
                    break;
                case SDL_EVENT_QUIT:
                    ProcessSdl3EventQuit();
                    break;
            }
        }

        private void ProcessSdl3EventQuit()
        {
            SendEvent(new EventQuit());
        }

        private void ProcessSdl3EventMouseWheel(in SDL_MouseWheelEvent ev)
        {
            SendEvent(new EventWheel(ev.windowID, ev.x, ev.y));
        }

        private void ProcessSdl3EventMouseButton(in SDL_MouseButtonEvent ev)
        {
            var mods = SDL_GetModState();
            SendEvent(new EventMouseButton(ev.windowID, ev.type, ev.button, mods));
        }

        private void ProcessSdl3EventMouseMotion(in SDL_MouseMotionEvent ev)
        {
            // _sawmill.Info($"{evMotion.x}, {evMotion.y}, {evMotion.xrel}, {evMotion.yrel}");
            SendEvent(new EventMouseMotion(ev.windowID, ev.x, ev.y, ev.xrel, ev.yrel));
        }

        private unsafe void ProcessSdl3EventTextInput(in SDL_TextInputEvent ev)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ev.text) ?? "";
            // _logManager.GetSawmill("ime").Debug($"Input: {str}");
            SendEvent(new EventText(ev.windowID, str));
        }

        private unsafe void ProcessSdl3EventTextEditing(in SDL_TextEditingEvent ev)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ev.text) ?? "";
            SendEvent(new EventTextEditing(ev.windowID, str, ev.start, ev.length));
        }

        private void ProcessSdl3EventKeyMapChanged()
        {
            ReloadKeyMap();
            SendEvent(new EventKeyMapChanged());
        }

        private void ProcessSdl3KeyEvent(in SDL_KeyboardEvent ev)
        {
            SendEvent(new EventKey(
                ev.windowID,
                ev.scancode,
                ev.type,
                ev.repeat,
                ev.mod));
        }

        private void ProcessSdl3EventWindowPixelSizeChanged(in SDL_WindowEvent ev)
        {
            var window = SDL_GetWindowFromID(ev.windowID);
            var width = ev.data1;
            var height = ev.data2;
            SDL_GetWindowSizeInPixels(window, out var fbW, out var fbH);

            SendEvent(new EventWindowPixelSize(ev.windowID, width, height, fbW, fbH));
        }

        private void ProcessSdl3EventWindowDisplayScaleChanged(in SDL_WindowEvent ev)
        {
            var window = SDL_GetWindowFromID(ev.windowID);
            var scale = SDL_GetWindowDisplayScale(window);

            SendEvent(new EventWindowContentScale(ev.windowID, scale));
        }

        private void ProcessSdl3EventWindowMisc(in SDL_WindowEvent ev)
        {
            SendEvent(new EventWindowMisc(ev.windowID, ev.type));
        }

        private abstract record EventBase;

        private record EventWindowCreate(
            Sdl3WindowCreateResult Result,
            TaskCompletionSource<Sdl3WindowCreateResult> Tcs
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
            float X,
            float Y,
            float XRel,
            float YRel
        ) : EventBase;

        private record EventMouseButton(
            uint WindowId,
            SDL_EventType Type,
            byte Button,
            SDL_Keymod Mods) : EventBase;

        private record EventText(
            uint WindowId,
            string Text
        ) : EventBase;

        private record EventTextEditing(
            uint WindowId,
            string Text,
            int Start,
            int Length
        ) : EventBase;

        private record EventWindowPixelSize(
            uint WindowId,
            int Width,
            int Height,
            int FramebufferWidth,
            int FramebufferHeight
        ) : EventBase;

        private record EventWindowContentScale(
            uint WindowId,
            float Scale
        ) : EventBase;

        private record EventWheel(
            uint WindowId,
            float XOffset,
            float YOffset
        ) : EventBase;

        // SDL_WindowEvents that don't have special handling.
        private record EventWindowMisc(
            uint WindowId,
            SDL_EventType EventId
        ) : EventBase;

        private record EventMonitorSetup(
            int Id,
            uint DisplayId,
            string Name,
            VideoMode CurrentMode,
            VideoMode[] AllModes
        ) : EventBase;

        private record EventMonitorDestroy(
            int Id
        ) : EventBase;

        private record EventKeyMapChanged : EventBase;

        private record EventQuit : EventBase;
    }
}
