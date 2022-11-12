using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;
using static SDL2.SDL.SDL_SYSWM_TYPE;
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
                    ProcessSdl2EventWindow(in ev.window);
                    break;
                case SDL_KEYDOWN:
                case SDL_KEYUP:
                    ProcessSdl2KeyEvent(in ev.key);
                    break;
                case SDL_TEXTINPUT:
                    ProcessSdl2EventTextInput(in ev.text);
                    break;
                case SDL_TEXTEDITING:
                    ProcessSdl2EventTextEditing(in ev.edit);
                    break;
                case SDL_TEXTEDITING_EXT:
                    ProcessSdl2EventTextEditingExt(in ev.editExt);
                    break;
                case SDL_MOUSEMOTION:
                    ProcessSdl2EventMouseMotion(in ev.motion);
                    break;
                case SDL_MOUSEBUTTONDOWN:
                case SDL_MOUSEBUTTONUP:
                    ProcessSdl2EventMouseButton(in ev.button);
                    break;
                case SDL_MOUSEWHEEL:
                    ProcessSdl2EventMouseWheel(in ev.wheel);
                    break;
                case SDL_DISPLAYEVENT:
                    ProcessSdl2EventDisplay(in ev.display);
                    break;
                case SDL_SYSWMEVENT:
                    ProcessSdl2EventSysWM(in ev.syswm);
                    break;
            }
        }

        private void ProcessSdl2EventDisplay(in SDL_DisplayEvent evDisplay)
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
                // _logManager.GetSawmill("ime").Debug($"Input: {str}");
                SendEvent(new EventText(ev.windowID, str));
            }
        }

        private unsafe void ProcessSdl2EventTextEditing(in SDL_TextEditingEvent ev)
        {
            fixed (byte* text = ev.text)
            {
                SendTextEditing(ev.windowID, text, ev.start, ev.length);
            }
        }

        private unsafe void ProcessSdl2EventTextEditingExt(in SDL_TextEditingExtEvent ev)
        {
            SendTextEditing(ev.windowID, (byte*) ev.text, ev.start, ev.length);
            SDL_free(ev.text);
        }

        private unsafe void SendTextEditing(uint window, byte* text, int start, int length)
        {
            var str = Marshal.PtrToStringUTF8((nint) text) ?? "";
            // _logManager.GetSawmill("ime").Debug($"Editing: '{str}', start: {start}, len: {length}");
            SendEvent(new EventTextEditing(window, str, start, length));
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

        // ReSharper disable once InconsistentNaming
        private unsafe void ProcessSdl2EventSysWM(in SDL_SysWMEvent ev)
        {
            ref readonly var sysWmMessage = ref *(SDL_SysWMmsg*)ev.msg;
            if (sysWmMessage.subsystem != SDL_SYSWM_WINDOWS)
                return;

            ref readonly var winMessage = ref *(SDL_SysWMmsgWin32*)ev.msg;
            if (winMessage.msg is WM.WM_KEYDOWN or WM.WM_KEYUP)
            {
                TryWin32VirtualVKey(in winMessage);
            }
        }

        private void TryWin32VirtualVKey(in SDL_SysWMmsgWin32 msg)
        {
            // Workaround for https://github.com/ocornut/imgui/issues/2977
            // This is gonna bite me in the ass if SDL2 ever fixes this upstream, isn't it...
            // (I spent disproportionate amounts of effort on this).

            // Code for V key.
            if ((int)msg.wParam is not (0x56 or VK.VK_CONTROL))
                return;

            var scanCode = (msg.lParam >> 16) & 0xFF;
            if (scanCode != 0)
                return;

            SendEvent(new EventWindowsFakeV(msg.hwnd, msg.msg, msg.wParam));
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

        private record EventTextEditing(
            uint WindowId,
            string Text,
            int Start,
            int Length
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

        private record EventWindowsFakeV(HWND Window,
            uint Message, WPARAM WParam) : EventBase;

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private struct SDL_SysWMmsg
        {
            public SDL_version version;
            public SDL_SYSWM_TYPE subsystem;
        }

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private struct SDL_SysWMmsgWin32
        {
            public SDL_version version;
            public SDL_SYSWM_TYPE subsystem;
            public HWND hwnd;
            public uint msg;
            public WPARAM wParam;
            public LPARAM lParam;
        }
    }
}
