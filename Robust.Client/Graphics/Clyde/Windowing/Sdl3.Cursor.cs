using System;
using System.Collections.Generic;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
    {
        private readonly Dictionary<ClydeHandle, WinThreadCursorReg> _winThreadCursors = new();
        private readonly CursorImpl[] _standardCursors = new CursorImpl[(int)StandardCursorShape.CountCursors];

        public ICursor CursorGetStandard(StandardCursorShape shape)
        {
            return _standardCursors[(int)shape];
        }

        public ICursor CursorCreate(Image<Rgba32> image, Vector2i hotSpot)
        {
            var cloneImg = new Image<Rgba32>(image.Width, image.Height);
            image.GetPixelSpan().CopyTo(cloneImg.GetPixelSpan());

            var id = _clyde.AllocRid();
            SendCmd(new CmdCursorCreate { Bytes = cloneImg, Hotspot = hotSpot, Cursor = id });

            return new CursorImpl(this, id, false);
        }

        private unsafe void WinThreadCursorCreate(CmdCursorCreate cmd)
        {
            using var img = cmd.Bytes;

            fixed (Rgba32* pixPtr = img.GetPixelSpan())
            {
                var surface = SDL.SDL_CreateSurfaceFrom(
                    img.Width,
                    img.Height,
                    SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                    (IntPtr)pixPtr,
                    sizeof(Rgba32) * img.Width);

                var cursor = SDL.SDL_CreateColorCursor(surface, cmd.Hotspot.X, cmd.Hotspot.Y);
                if (cursor == 0)
                    throw new InvalidOperationException("SDL_CreateColorCursor failed");

                _winThreadCursors.Add(cmd.Cursor, new WinThreadCursorReg { Ptr = cursor });

                SDL.SDL_DestroySurface(surface);
            }
        }

        public void CursorSet(WindowReg window, ICursor? cursor)
        {
            CheckWindowDisposed(window);

            // SDL_SetCursor(NULL) does redraw, not reset.
            cursor ??= CursorGetStandard(StandardCursorShape.Arrow);

            var reg = (Sdl3WindowReg)window;

            if (reg.Cursor == cursor)
                return;

            var impl = (CursorImpl)cursor;
            DebugTools.Assert(impl.Owner == this);

            if (impl.Id == default)
                throw new ObjectDisposedException(nameof(cursor));

            reg.Cursor = impl;
            SendCmd(new CmdWinCursorSet { Window = reg.Sdl3Window, Cursor = impl.Id });
        }

        private void WinThreadWinCursorSet(CmdWinCursorSet cmd)
        {
            var window = cmd.Window;
            var ptr = _winThreadCursors[cmd.Cursor].Ptr;

            // TODO: multi-window??
            SDL.SDL_SetCursor(ptr);
        }

        private void InitCursors()
        {
            Add(StandardCursorShape.Arrow, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);
            Add(StandardCursorShape.IBeam, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT);
            Add(StandardCursorShape.Crosshair, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR);
            Add(StandardCursorShape.Hand, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER);
            Add(StandardCursorShape.HResize, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE);
            Add(StandardCursorShape.VResize, SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE);

            void Add(StandardCursorShape shape, SDL.SDL_SystemCursor sysCursor)
            {
                var id = _clyde.AllocRid();
                var cursor = SDL.SDL_CreateSystemCursor(sysCursor);

                var impl = new CursorImpl(this, id, true);

                _standardCursors[(int)shape] = impl;
                _winThreadCursors.Add(id, new WinThreadCursorReg { Ptr = cursor });
            }
        }

        private void WinThreadCursorDestroy(CmdCursorDestroy cmd)
        {
            if (!_winThreadCursors.TryGetValue(cmd.Cursor, out var cursor))
                return;

            SDL.SDL_DestroyCursor(cursor.Ptr);
        }

        private sealed class CursorImpl : ICursor
        {
            private readonly bool _standard;
            public Sdl3WindowingImpl Owner { get; }
            public ClydeHandle Id { get; private set; }

            public CursorImpl(Sdl3WindowingImpl clyde, ClydeHandle id, bool standard)
            {
                _standard = standard;
                Owner = clyde;
                Id = id;
            }

            ~CursorImpl()
            {
                DisposeImpl();
            }

            private void DisposeImpl()
            {
                Owner.SendCmd(new CmdCursorDestroy { Cursor = Id });
                Id = default;
            }

            public void Dispose()
            {
                if (_standard)
                {
                    throw new InvalidOperationException("Can't dispose standard cursor shape.");
                }

                GC.SuppressFinalize(this);
                DisposeImpl();
            }
        }

        public sealed class WinThreadCursorReg
        {
            public nint Ptr;
        }
    }
}
