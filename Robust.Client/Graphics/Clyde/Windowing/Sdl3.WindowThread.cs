using System;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Maths;
using SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
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
                var res = SDL.SDL_WaitEventRef(ref Unsafe.NullRef<SDL.SDL_Event>());
                if (!res)
                {
                    _sawmill.Error("Error while waiting on SDL3 events: {error}", SDL.SDL_GetError());
                    continue; // Assume it's a transient failure?
                }

                while (SDL.SDL_PollEvent(out _))
                {
                    // We let callbacks process all events because of stuff like resizing.
                }

                while (_cmdReader.TryRead(out var cmd) && _windowingRunning)
                {
                    ProcessSdl3Cmd(cmd);
                }
            }
        }

        public void PollEvents()
        {
            while (SDL.SDL_PollEvent(out _))
            {
                // We let callbacks process all events because of stuff like resizing.
            }
        }

        private void ProcessSdl3Cmd(CmdBase cmdb)
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

                case CmdWinSetSize cmd:
                    WinThreadWinSetSize(cmd);
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

                case CmdWinWinSetFullscreen cmd:
                    WinThreadWinSetFullscreen(cmd);
                    break;

                case CmdWinSetWindowed cmd:
                    WinThreadWinSetWindowed(cmd);
                    break;

                case CmdTextInputSetRect cmd:
                    WinThreadSetTextInputRect(cmd);
                    break;

                case CmdTextInputStart cmd:
                    WinThreadStartTextInput(cmd);
                    break;

                case CmdTextInputStop cmd:
                    WinThreadStopTextInput(cmd);
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
                SingleWriter = true,
                // For unblocking continuations.
                AllowSynchronousContinuations = true
            });

            _eventReader = eventChannel.Reader;
            _eventWriter = eventChannel.Writer;
        }

        private void SendCmd(CmdBase cmd)
        {
            if (_clyde._threadWindowApi)
            {
                _cmdWriter.TryWrite(cmd);

                SDL.SDL_Event ev = default;
                ev.type = _sdlEventWakeup;
                // Post empty event to unstuck WaitEvents if necessary.
                // This self-registered event type is ignored by the winthread, but it'll still wake it up.

                // This can fail if the event queue is full.
                // That's not really a problem since in that case something else will be sure to wake the thread up anyways.
                // NOTE: have to avoid using PushEvents since that invokes callbacks which causes a deadlock.
                SDL.SDL_PeepEvents(new Span<SDL.SDL_Event>(ref ev), 1, SDL.SDL_EventAction.SDL_ADDEVENT, ev.type, ev.type);
            }
            else
            {
                ProcessSdl3Cmd(cmd);
            }
        }

        private void SendEvent(EventBase ev)
        {
            if (_clyde._threadWindowApi)
            {
                var task = _eventWriter.WriteAsync(ev);

                if (!task.IsCompletedSuccessfully)
                {
                    task.AsTask().Wait();
                }
            }
            else
            {
                ProcessEvent(ev);
            }
        }


        private abstract class CmdBase;

        private sealed class CmdTerminate : CmdBase;

        private sealed class CmdWinCreate : CmdBase
        {
            public required GLContextSpec? GLSpec;
            public required WindowCreateParameters Parameters;
            public required nint ShareWindow;
            public required nint ShareContext;
            public required nint OwnerWindow;
            public required TaskCompletionSource<Sdl3WindowCreateResult> Tcs;
        }

        private sealed class CmdWinDestroy : CmdBase
        {
            public nint Window;
            public bool HadOwner;
        }

        private sealed class Sdl3WindowCreateResult
        {
            public Sdl3WindowReg? Reg;
            public string? Error;
        }

        private sealed class CmdRunAction : CmdBase
        {
            public required Action Action;
        }

        private sealed class CmdSetClipboard : CmdBase
        {
            public required string Text;
        }

        private sealed class CmdGetClipboard : CmdBase
        {
            public required TaskCompletionSource<string> Tcs;
        }

        private sealed class CmdWinRequestAttention : CmdBase
        {
            public nint Window;
        }

        private sealed class CmdWinSetSize : CmdBase
        {
            public nint Window;
            public int W;
            public int H;
        }

        private sealed class CmdWinSetVisible : CmdBase
        {
            public nint Window;
            public bool Visible;
        }

        private sealed class CmdWinSetTitle : CmdBase
        {
            public nint Window;
            public required string Title;
        }

        private sealed class CmdCursorCreate : CmdBase
        {
            public required Image<Rgba32> Bytes;
            public Vector2i Hotspot;
            public ClydeHandle Cursor;
        }

        private sealed class CmdCursorDestroy : CmdBase
        {
            public ClydeHandle Cursor;
        }

        private sealed class CmdWinCursorSet : CmdBase
        {
            public nint Window;
            public ClydeHandle Cursor;
        }

        private sealed class CmdWinWinSetFullscreen : CmdBase
        {
            public nint Window;
        }

        private sealed class CmdWinSetWindowed : CmdBase
        {
            public nint Window;
            public int Width;
            public int Height;
            public int PosX;
            public int PosY;
        }

        // IME
        private sealed class CmdTextInputStart : CmdBase
        {
            public nint Window;
        }

        private sealed class CmdTextInputStop : CmdBase
        {
            public nint Window;
        }

        private sealed class CmdTextInputSetRect : CmdBase
        {
            public nint Window;
            public SDL.SDL_Rect Rect;
            public int Cursor;
        }
    }
}
