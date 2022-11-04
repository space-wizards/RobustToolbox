using System;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static SDL2.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        private bool _windowingRunning;
        private ChannelWriter<CmdBase> _cmdWriter = default!;
        private ChannelReader<CmdBase> _cmdReader = default!;

        private ChannelReader<EventBase> _eventReader = default!;
        private ChannelWriter<EventBase> _eventWriter = default!;

        private uint _sdlEventWakeup;

        public void EnterWindowLoop()
        {
            _windowingRunning = true;

            while (_windowingRunning)
            {
                var res = SDL_WaitEvent(out Unsafe.NullRef<SDL_Event>());
                if (res == 0)
                {
                    _sawmill.Error("Error while waiting on SDL2 events: {error}", SDL_GetError());
                    continue; // Assume it's a transient failure?
                }

                while (SDL_PollEvent(out _) == 1)
                {
                    // We let callbacks process all events because of stuff like resizing.
                }

                while (_cmdReader.TryRead(out var cmd) && _windowingRunning)
                {
                    ProcessSdl2Cmd(cmd);
                }
            }
        }

        private void ProcessSdl2Cmd(CmdBase cmdb)
        {
            switch (cmdb)
            {
                case CmdTerminate:
                    _windowingRunning = false;
                    _eventWriter.Complete();
                    break;

                case CmdWinCreate cmd:
                    WinThreadWinCreate(cmd);
                    break;

                case CmdWinDestroy cmd:
                    WinThreadWinDestroy(cmd);
                    break;

                case CmdRunAction cmd:
                    cmd.Action();
                    break;

                case CmdWinSetTitle cmd:
                    WinThreadWinSetTitle(cmd);
                    break;

                case CmdSetClipboard cmd:
                    WinThreadSetClipboard(cmd);
                    break;

                case CmdGetClipboard cmd:
                    WinThreadGetClipboard(cmd);
                    break;

                case CmdWinRequestAttention cmd:
                    WinThreadWinRequestAttention(cmd);
                    break;

                case CmdWinSetVisible cmd:
                    WinThreadWinSetVisible(cmd);
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

                case CmdWinWinSetMode cmd:
                    WinThreadWinSetMode(cmd);
                    break;

                case CmdTextInputSetRect cmd:
                    WinThreadSetTextInputRect(cmd);
                    break;

                case CmdTextInputStart:
                    WinThreadStartTextInput();
                    break;

                case CmdTextInputStop:
                    WinThreadStopTextInput();
                    break;
            }
        }

        public void TerminateWindowLoop()
        {
            SendCmd(new CmdTerminate());
            _cmdWriter.Complete();

            // Drain command queue ignoring it until the window thread confirms completion.
#pragma warning disable RA0004
            while (_eventReader.WaitToReadAsync().AsTask().Result)
#pragma warning restore RA0004
            {
                _eventReader.TryRead(out _);
            }
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

        private unsafe void SendCmd(CmdBase cmd)
        {
            _cmdWriter.TryWrite(cmd);

            SDL_Event ev = default;
            ev.type = (SDL_EventType)_sdlEventWakeup;
            // Post empty event to unstuck WaitEvents if necessary.
            // This self-registered event type is ignored by the winthread, but it'll still wake it up.

            // This can fail if the event queue is full.
            // That's not really a problem since in that case something else will be sure to wake the thread up anyways.
            // NOTE: have to avoid using PushEvents since that invokes callbacks which causes a deadlock.
            SDL_PeepEvents(&ev, 1, SDL_eventaction.SDL_ADDEVENT, ev.type, ev.type);
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

        private sealed record CmdWinCreate(
            GLContextSpec? GLSpec,
            WindowCreateParameters Parameters,
            nint ShareWindow,
            nint ShareContext,
            nint OwnerWindow,
            TaskCompletionSource<Sdl2WindowCreateResult> Tcs
        ) : CmdBase;

        private sealed record CmdWinDestroy(
            nint Window,
            bool HadOwner
        ) : CmdBase;

        private sealed record Sdl2WindowCreateResult(
            Sdl2WindowReg? Reg,
            string? Error
        );

        private sealed record CmdRunAction(
            Action Action
        ) : CmdBase;

        private sealed record CmdSetClipboard(
            string Text
        ) : CmdBase;

        private sealed record CmdGetClipboard(
            TaskCompletionSource<string> Tcs
        ) : CmdBase;

        private sealed record CmdWinRequestAttention(
            nint Window
        ) : CmdBase;

        private sealed record CmdWinSetVisible(
            nint Window,
            bool Visible
        ) : CmdBase;

        private sealed record CmdWinSetTitle(
            nint Window,
            string Title
        ) : CmdBase;

        private sealed record CmdCursorCreate(
            Image<Rgba32> Bytes,
            Vector2i Hotspot,
            ClydeHandle Cursor
        ) : CmdBase;

        private sealed record CmdCursorDestroy(
            ClydeHandle Cursor
        ) : CmdBase;

        private sealed record CmdWinCursorSet(
            nint Window,
            ClydeHandle Cursor
        ) : CmdBase;

        private sealed record CmdWinWinSetMode(
            nint Window,
            WindowMode Mode
        ) : CmdBase;

        // IME
        private sealed record CmdTextInputStart : CmdBase
        {
            public static readonly CmdTextInputStart Instance = new();
        }

        private sealed record CmdTextInputStop : CmdBase
        {
            public static readonly CmdTextInputStop Instance = new();
        }

        private sealed record CmdTextInputSetRect(
            SDL_Rect Rect
        ) : CmdBase;
    }
}
