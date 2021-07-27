using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Shared;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed partial class GlfwWindowingImpl
        {
            private bool _windowingRunning;
            private ChannelWriter<CmdBase> _cmdWriter = default!;
            private ChannelReader<CmdBase> _cmdReader = default!;

            private ChannelReader<EventBase> _eventReader = default!;
            private ChannelWriter<EventBase> _eventWriter = default!;

            //
            // Let it be forever recorded that I started work on windowing thread separation
            // because win32 SetCursor was taking 15ms spinwaiting inside the kernel.
            //

            //
            // To avoid stutters and solve some other problems like smooth window resizing,
            // we (by default) use a separate thread for windowing.
            //
            // Types like WindowReg are considered to be part of the "game" thread
            // and should **NOT** be directly updated/accessed from the windowing thread.
            //
            // Got that?
            //

            //
            // The windowing -> game channel is bounded so that the OS properly detects the game as locked
            // up when it actually locks up. The other way around is not bounded to avoid deadlocks.
            // This also means that all operations like clipboard reading, window creation, etc....
            // have to be asynchronous.
            //

            public void EnterWindowLoop()
            {
                _windowingRunning = true;

                while (_windowingRunning)
                {
                    // glfwPostEmptyEvent is broken on macOS and crashes when not called from the main thread
                    // (despite what the docs claim, and yes this makes it useless).
                    // Because of this, we just forego it and use glfwWaitEventsTimeout on macOS instead.
                    if (OperatingSystem.IsMacOS())
                        GLFW.WaitEventsTimeout(0.008);
                    else
                        GLFW.WaitEvents();

                    while (_cmdReader.TryRead(out var cmd))
                    {
                        ProcessGlfwCmd(cmd);
                    }
                }
            }

            private void ProcessGlfwCmd(CmdBase cmdb)
            {
                switch (cmdb)
                {
                    case CmdTerminate:
                        _windowingRunning = false;
                        break;

                    case CmdWinSetTitle cmd:
                        WinThreadWinSetTitle(cmd);
                        break;

                    case CmdWinSetMonitor cmd:
                        WinThreadWinSetMonitor(cmd);
                        break;

                    case CmdWinSetVisible cmd:
                        WinThreadWinSetVisible(cmd);
                        break;

                    case CmdWinRequestAttention cmd:
                        WinThreadWinRequestAttention(cmd);
                        break;

                    case CmdWinSetFullscreen cmd:
                        WinThreadWinSetFullscreen(cmd);
                        break;

                    case CmdWinCreate cmd:
                        WinThreadWinCreate(cmd);
                        break;

                    case CmdWinDestroy cmd:
                        WinThreadWinDestroy(cmd);
                        break;

                    case CmdSetClipboard cmd:
                        WinThreadSetClipboard(cmd);
                        break;

                    case CmdGetClipboard cmd:
                        WinThreadGetClipboard(cmd);
                        break;

                    case CmdCursorCreate cmd:
                        WinThreadCursorCreate(cmd);
                        break;

                    case CmdCursorDestroy cmd:
                        WinThreadCursorDestroy(cmd);
                        break;

                    case CmdWinCursorSet cmd:
                        WinThreadWinCursorSet(cmd);
                        break;
                }
            }

            public void TerminateWindowLoop()
            {
                SendCmd(new CmdTerminate());
            }

            private void InitChannels()
            {
                var cmdChannel = Channel.CreateUnbounded<CmdBase>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    // Finalizers can write to this in some cases.
                    SingleWriter = false
                });

                _cmdReader = cmdChannel.Reader;
                _cmdWriter = cmdChannel.Writer;

                var bufferSize = _cfg.GetCVar(CVars.DisplayInputBufferSize);
                var eventChannel = Channel.CreateBounded<EventBase>(new BoundedChannelOptions(bufferSize)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });

                _eventReader = eventChannel.Reader;
                _eventWriter = eventChannel.Writer;
            }

            private void SendCmd(CmdBase cmd)
            {
                _cmdWriter.TryWrite(cmd);

                // Post empty event to unstuck WaitEvents if necessary.
                if (!OperatingSystem.IsMacOS())
                    GLFW.PostEmptyEvent();
            }

            private void SendEvent(EventBase ev)
            {
                var task = _eventWriter.WriteAsync(ev);

                if (!task.IsCompletedSuccessfully)
                {
                    task.AsTask().Wait();
                }
            }

            private abstract record CmdBase;

            private sealed record CmdTerminate : CmdBase;

            private sealed record CmdWinSetTitle(
                nint Window,
                string Title
            ) : CmdBase;

            private sealed record CmdWinSetMonitor(
                nint Window,
                int MonitorId,
                int X, int Y,
                int W, int H,
                int RefreshRate
            ) : CmdBase;

            private sealed record CmdWinMaximize(
                nint Window
            ) : CmdBase;

            private sealed record CmdWinSetFullscreen(
                nint Window
            ) : CmdBase;

            private sealed record CmdWinSetVisible(
                nint Window,
                bool Visible
            ) : CmdBase;

            private sealed record CmdWinRequestAttention(
                nint Window
            ) : CmdBase;

            private sealed record CmdWinCreate(
                GLContextSpec? GLSpec,
                WindowCreateParameters Parameters,
                nint ShareWindow,
                TaskCompletionSource<GlfwWindowCreateResult> Tcs
            ) : CmdBase;

            private sealed record CmdWinDestroy(
                nint Window
            ) : CmdBase;

            private sealed record GlfwWindowCreateResult(
                GlfwWindowReg? Reg,
                (string Desc, ErrorCode Code)? Error
            );

            private sealed record CmdSetClipboard(
                nint Window,
                string Text
            ) : CmdBase;

            private sealed record CmdGetClipboard(
                nint Window,
                TaskCompletionSource<string> Tcs
            ) : CmdBase;

            private sealed record CmdWinCursorSet(
                nint Window,
                ClydeHandle Cursor
            ) : CmdBase;

            private sealed record CmdCursorCreate(
                Image<Rgba32> Bytes,
                Vector2i Hotspot,
                ClydeHandle Cursor
            ) : CmdBase;

            private sealed record CmdCursorDestroy(
                ClydeHandle Cursor
            ) : CmdBase;
        }
    }
}
