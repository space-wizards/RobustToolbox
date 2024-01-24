using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Robust.Shared.Maths;
using TerraFX.Interop.Windows;
using SDL;
using static SDL.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class SdlWindowingImpl
    {
        private static int EventWatch(IntPtr userdata, IntPtr eventPointer)
        {
            var obj = (SdlWindowingImpl) GCHandle.FromIntPtr(userdata).Target!;
            var ev = Marshal.PtrToStructure<SDL_Event>(eventPointer);

            obj.ProcessSdl2Event(in ev);
            return 0;
        }

        private void ProcessSdl2Event(in SDL_Event ev)
        {
            // old "SDL_WINDOWEVENT" event range
            if (ev.type is >= SDL_EventType.WindowFirst and <= SDL_EventType.WindowLast)
            {
                ProcessSdl2EventWindow(in ev.window);
                return;
            }
            // old "SDL_DISPLAYEVENT" event range
            if (ev.type is >= SDL_EventType.DisplayFirst and <= SDL_EventType.DisplayLast)
            {
                ProcessSdl2EventDisplay(in ev.display);
                return;
            }

            switch (ev.type)
            {
                case SDL_EventType.KeyDown:
                case SDL_EventType.KeyUp:
                    ProcessSdl2KeyEvent(ev.key);
                    break;
                case SDL_EventType.TextInput:
                    ProcessSdl2EventTextInput(in ev.text);
                    break;
                case SDL_EventType.TextEditing:
                    ProcessSdl2EventTextEditing(in ev.edit);
                    break;
                case SDL_EventType.KeymapChanged:
                    ProcessSdl2EventKeyMapChanged();
                    break;
                // case SDL_TEXTEDITING_EXT:
                //     ProcessSdl2EventTextEditingExt(ev.editExt);
                //     break;
                // case SDL_SYSWMEVENT:
                //     ProcessSdl2EventSysWM(in ev.syswm);
                //     break;
                case SDL_EventType.MouseMotion:
                    ProcessSdl2EventMouseMotion(in ev.motion);
                    break;
                case SDL_EventType.MouseButtonDown:
                case SDL_EventType.MouseButtonUp:
                    ProcessSdl2EventMouseButton(in ev.button);
                    break;
                case SDL_EventType.MouseWheel:
                    ProcessSdl2EventMouseWheel(in ev.wheel);
                    break;
                case SDL_EventType.Quit:
                    ProcessSdl2EventQuit();
                    break;
            }
        }

        private void ProcessSdl2EventQuit()
        {
            SendEvent(new EventQuit());
        }

        private void ProcessSdl2EventDisplay(in SDL_DisplayEvent evDisplay)
        {
            switch ((SDL_EventType)evDisplay.type)
            {
                case SDL_EventType.DisplayAdded:
                    WinThreadSetupMonitor(evDisplay.displayID);
                    break;
                case SDL_EventType.DisplayRemoved:
                    WinThreadDestroyMonitor(evDisplay.displayID);
                    break;
            }
        }

        private void ProcessSdl2EventMouseWheel(in SDL_MouseWheelEvent ev)
        {
            SendEvent(new EventWheel(ev.windowID, ev.x, ev.y));
        }

        private void ProcessSdl2EventMouseButton(in SDL_MouseButtonEvent ev)
        {
            SendEvent(new EventMouseButton(ev.windowID, (SDL_EventType)ev.type, ev.button));
        }

        private void ProcessSdl2EventMouseMotion(in SDL_MouseMotionEvent ev)
        {
            // _sawmill.Info($"{evMotion.x}, {evMotion.y}, {evMotion.xrel}, {evMotion.yrel}");
            SendEvent(new EventMouseMotion(ev.windowID,
                (int)Math.Round(ev.x), (int)Math.Round(ev.y),
                (int)Math.Round(ev.xrel), (int)Math.Round(ev.yrel)));
        }

        private unsafe void ProcessSdl2EventTextInput(in SDL_TextInputEvent ev)
        {
            var str = GetStringOrEmpty(ev.text);
            // _logManager.GetSawmill("ime").Debug($"Input: {str}");
            SendEvent(new EventText(ev.windowID, str));
        }

        private unsafe void ProcessSdl2EventTextEditing(in SDL_TextEditingEvent ev)
        {
            var str = GetStringOrEmpty(ev.text);
            // _logManager.GetSawmill("ime").Debug($"Editing: '{str}', start: {start}, len: {length}");
            SendEvent(new EventTextEditing(ev.windowID, str, ev.start, ev.length));
        }

        // TODO FIXME
        // private unsafe void ProcessSdl2EventTextEditingExt(in SDL_TextEditingExtEvent ev)
        // {
        //     SendTextEditing(ev.windowID, (byte*) ev.text, ev.start, ev.length);
        //     SDL_free(ev.text);
        // }

        private void ProcessSdl2EventKeyMapChanged()
        {
            ReloadKeyMap();
            SendEvent(new EventKeyMapChanged());
        }

        private void ProcessSdl2KeyEvent(in SDL_KeyboardEvent ev)
        {
            SendEvent(new EventKey(
                ev.windowID,
                ev.keysym.scancode,
                (SDL_EventType) ev.type,
                ev.repeat != 0,
                ev.keysym.mod));
        }

        private void ProcessSdl2EventWindow(in SDL_WindowEvent ev)
        {
            var window = SDL_GetWindowFromID(ev.windowID);

            switch ((SDL_EventType) ev.type)
            {
                case SDL_EventType.WindowPixelSizeChanged:
                    var width = ev.data1;
                    var height = ev.data2;
                    SDL_GetWindowSizeInPixels(window, out var fbW, out var fbH);
                    var (xScale, yScale) = GetWindowScale(window);

                    _sawmill.Debug($"{width}x{height}, {fbW}x{fbH}, {xScale}x{yScale}");

                    SendEvent(new EventWindowSize(ev.windowID, width, height, fbW, fbH, xScale, yScale));
                    break;

                default:
                    SendEvent(new EventWindow(ev.windowID, (SDL_EventType)ev.type));
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        // private unsafe void ProcessSdl2EventSysWM(in SDL_SysWMEvent ev)
        // {
        //     ref readonly var sysWmMessage = ref *(SDL_SysWMmsg*)ev.msg;
        //     if (sysWmMessage.subsystem != SDL_SYSWM_WINDOWS)
        //         return;
        //
        //     ref readonly var winMessage = ref *(SDL_SysWMmsgWin32*)ev.msg;
        //     if (winMessage.msg is WM.WM_KEYDOWN or WM.WM_KEYUP)
        //     {
        //         TryWin32VirtualVKey(in winMessage);
        //     }
        // }

        // TODO verify if this is fixed sdl (#7924) maybe?
        // private void TryWin32VirtualVKey(in SDL_SysWMmsgWin32 msg)
        // {
        //     // Workaround for https://github.com/ocornut/imgui/issues/2977
        //     // This is gonna bite me in the ass if SDL2 ever fixes this upstream, isn't it...
        //     // (I spent disproportionate amounts of effort on this).
        //
        //     // Code for V key.
        //     if ((int)msg.wParam is not (0x56 or VK.VK_CONTROL))
        //         return;
        //
        //     var scanCode = (msg.lParam >> 16) & 0xFF;
        //     if (scanCode != 0)
        //         return;
        //
        //     SendEvent(new EventWindowsFakeV(msg.hwnd, msg.msg, msg.wParam));
        // }

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
            ushort Mods
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
            SDL_EventType EventId
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

        private record EventKeyMapChanged : EventBase;
        private record EventQuit : EventBase;

        // [StructLayout(LayoutKind.Sequential)]
        // [SuppressMessage("ReSharper", "InconsistentNaming")]
        // [SuppressMessage("ReSharper", "IdentifierTypo")]
        // [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        // [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        // private struct SDL_SysWMmsg
        // {
        //     public SDL_version version;
        //     public SDL_SYSWM_TYPE subsystem;
        // }
        //
        // [StructLayout(LayoutKind.Sequential)]
        // [SuppressMessage("ReSharper", "InconsistentNaming")]
        // [SuppressMessage("ReSharper", "IdentifierTypo")]
        // [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        // [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        // private struct SDL_SysWMmsgWin32
        // {
        //     public SDL.SDL.SDL_version version;
        //     public SDL_SYSWM_TYPE subsystem;
        //     public HWND hwnd;
        //     public uint msg;
        //     public WPARAM wParam;
        //     public LPARAM lParam;
        // }
    }
}
