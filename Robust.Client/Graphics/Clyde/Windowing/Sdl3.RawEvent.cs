using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SDL3;
using ET = SDL3.SDL.SDL_EventType;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
    {
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe byte EventWatch(void* userdata, SDL.SDL_Event* sdlevent)
        {
            var obj = (Sdl3WindowingImpl)GCHandle.FromIntPtr((IntPtr)userdata).Target!;

            obj.ProcessSdl3Event(in *sdlevent);

            return 0;
        }

        private void ProcessSdl3Event(in SDL.SDL_Event ev)
        {
            switch ((ET)ev.type)
            {
                case ET.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                    ProcessSdl3EventWindowPixelSizeChanged(in ev.window);
                    break;
                case ET.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
                    ProcessSdl3EventWindowDisplayScaleChanged(in ev.window);
                    break;
                case ET.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                case ET.SDL_EVENT_WINDOW_MOUSE_ENTER:
                case ET.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                case ET.SDL_EVENT_WINDOW_MINIMIZED:
                case ET.SDL_EVENT_WINDOW_RESTORED:
                case ET.SDL_EVENT_WINDOW_FOCUS_GAINED:
                case ET.SDL_EVENT_WINDOW_FOCUS_LOST:
                case ET.SDL_EVENT_WINDOW_MOVED:
                    ProcessSdl3EventWindowMisc(in ev.window);
                    break;
                case ET.SDL_EVENT_KEY_DOWN:
                case ET.SDL_EVENT_KEY_UP:
                    ProcessSdl3KeyEvent(in ev.key);
                    break;
                case ET.SDL_EVENT_TEXT_INPUT:
                    ProcessSdl3EventTextInput(in ev.text);
                    break;
                case ET.SDL_EVENT_TEXT_EDITING:
                    ProcessSdl3EventTextEditing(in ev.edit);
                    break;
                case ET.SDL_EVENT_KEYMAP_CHANGED:
                    ProcessSdl3EventKeyMapChanged();
                    break;
                case ET.SDL_EVENT_MOUSE_MOTION:
                    ProcessSdl3EventMouseMotion(in ev.motion);
                    break;
                case ET.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case ET.SDL_EVENT_MOUSE_BUTTON_UP:
                    ProcessSdl3EventMouseButton(in ev.button);
                    break;
                case ET.SDL_EVENT_MOUSE_WHEEL:
                    ProcessSdl3EventMouseWheel(in ev.wheel);
                    break;
                case ET.SDL_EVENT_DISPLAY_ADDED:
                    WinThreadSetupMonitor(ev.display.displayID);
                    break;
                case ET.SDL_EVENT_DISPLAY_REMOVED:
                    WinThreadDestroyMonitor(ev.display.displayID);
                    break;
                case ET.SDL_EVENT_QUIT:
                    ProcessSdl3EventQuit();
                    break;
            }
        }

        private void ProcessSdl3EventQuit()
        {
            SendEvent(new EventQuit());
        }

        private void ProcessSdl3EventMouseWheel(in SDL.SDL_MouseWheelEvent ev)
        {
            SendEvent(new EventWheel { WindowId = ev.windowID, XOffset = ev.x, YOffset = ev.y });
        }

        private void ProcessSdl3EventMouseButton(in SDL.SDL_MouseButtonEvent ev)
        {
            var mods = SDL.SDL_GetModState();
            SendEvent(new EventMouseButton
            {
                WindowId = ev.windowID,
                Type = ev.type,
                Button = ev.button,
                Mods = mods
            });
        }

        private void ProcessSdl3EventMouseMotion(in SDL.SDL_MouseMotionEvent ev)
        {
            SendEvent(new EventMouseMotion
            {
                WindowId = ev.windowID,
                X = ev.x,
                Y = ev.y,
                XRel = ev.xrel,
                YRel = ev.yrel
            });
        }

        private unsafe void ProcessSdl3EventTextInput(in SDL.SDL_TextInputEvent ev)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ev.text) ?? "";
            SendEvent(new EventText { WindowId = ev.windowID, Text = str });
        }

        private unsafe void ProcessSdl3EventTextEditing(in SDL.SDL_TextEditingEvent ev)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ev.text) ?? "";
            SendEvent(new EventTextEditing
            {
                WindowId = ev.windowID,
                Text = str,
                Start = ev.start,
                Length = ev.length
            });
        }

        private void ProcessSdl3EventKeyMapChanged()
        {
            ReloadKeyMap();
            SendEvent(new EventKeyMapChanged());
        }

        private void ProcessSdl3KeyEvent(in SDL.SDL_KeyboardEvent ev)
        {
            SendEvent(new EventKey
            {
                WindowId = ev.windowID,
                Scancode = ev.scancode,
                Type = ev.type,
                Repeat = ev.repeat,
                Mods = ev.mod,
            });
        }

        private void ProcessSdl3EventWindowPixelSizeChanged(in SDL.SDL_WindowEvent ev)
        {
            var window = SDL.SDL_GetWindowFromID(ev.windowID);
            SDL.SDL_GetWindowSize(window, out var width, out var height);
            var fbW = ev.data1;
            var fbH = ev.data2;

            SendEvent(new EventWindowPixelSize
            {
                WindowId = ev.windowID,
                Width = width,
                Height = height,
                FramebufferWidth = fbW,
                FramebufferHeight = fbH,
            });
        }

        private void ProcessSdl3EventWindowDisplayScaleChanged(in SDL.SDL_WindowEvent ev)
        {
            var window = SDL.SDL_GetWindowFromID(ev.windowID);
            var scale = SDL.SDL_GetWindowDisplayScale(window);

            SendEvent(new EventWindowContentScale { WindowId = ev.windowID, Scale = scale });
        }

        private void ProcessSdl3EventWindowMisc(in SDL.SDL_WindowEvent ev)
        {
            SendEvent(new EventWindowMisc
            {
                WindowId = ev.windowID,
                EventId = ev.type,
                Data1 = ev.data1,
                Data2 = ev.data2
            });
        }

        private abstract class EventBase;

        private sealed class EventWindowCreate : EventBase
        {
            public required Sdl3WindowCreateResult Result;
            public required TaskCompletionSource<Sdl3WindowCreateResult> Tcs;
        }

        private sealed class EventKey : EventBase
        {
            public uint WindowId;
            public SDL.SDL_Scancode Scancode;
            public ET Type;
            public bool Repeat;
            public SDL.SDL_Keymod Mods;
        }

        private sealed class EventMouseMotion : EventBase
        {
            public uint WindowId;
            public float X;
            public float Y;
            public float XRel;
            public float YRel;
        }

        private sealed class EventMouseButton : EventBase
        {
            public uint WindowId;
            public ET Type;
            public byte Button;
            public SDL.SDL_Keymod Mods;
        }

        private sealed class EventText : EventBase
        {
            public uint WindowId;
            public required string Text;
        }

        private sealed class EventTextEditing : EventBase
        {
            public uint WindowId;
            public required string Text;
            public int Start;
            public int Length;
        }

        private sealed class EventWindowPixelSize : EventBase
        {
            public uint WindowId;
            public int Width;
            public int Height;
            public int FramebufferWidth;
            public int FramebufferHeight;
        }

        private sealed class EventWindowContentScale : EventBase
        {
            public uint WindowId;
            public float Scale;
        }

        private sealed class EventWheel : EventBase
        {
            public uint WindowId;
            public float XOffset;
            public float YOffset;
        }

        // SDL_WindowEvents that don't need any handling on the window thread itself.
        private sealed class EventWindowMisc : EventBase
        {
            public uint WindowId;
            public ET EventId;
            public int Data1;
            public int Data2;
        }

        private sealed class EventMonitorSetup : EventBase
        {
            public int Id;
            public uint DisplayId;
            public required string Name;
            public VideoMode CurrentMode;
            public required VideoMode[] AllModes;
        }

        private sealed class EventMonitorDestroy : EventBase
        {
            public int Id;
        }

        private sealed class EventKeyMapChanged : EventBase;

        private sealed class EventQuit : EventBase;
    }
}
