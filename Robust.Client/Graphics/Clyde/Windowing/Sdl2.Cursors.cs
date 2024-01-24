using System;
using System.Collections.Generic;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SDL;
using static SDL.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class SdlWindowingImpl : IWindowingImpl
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
            SendCmd(new CmdCursorCreate(cloneImg, hotSpot, id));

            return new CursorImpl(this, id, false);
        }

        private unsafe void WinThreadCursorCreate(CmdCursorCreate cmd)
        {
            var (img, (hotX, hotY), id) = cmd;

            fixed (Rgba32* pixPtr = img.GetPixelSpan())
            {
                var surface = SDL_CreateSurfaceFrom((IntPtr)pixPtr, img.Width, img.Height, 0, SDL_PixelFormatEnum.Rgba8888);
                var cursor = SDL_CreateColorCursor(surface, hotX, hotY);

                _winThreadCursors.Add(id, new WinThreadCursorReg { Ptr = cursor->Handle });

                SDL_DestroySurface(surface);
            }

            img.Dispose();
        }

        public void CursorSet(WindowReg window, ICursor? cursor)
        {
            CheckWindowDisposed(window);

            // SDL_SetCursor(NULL) does redraw, not reset.
            cursor ??= CursorGetStandard(StandardCursorShape.Arrow);

            var reg = (Sdl2WindowReg)window;

            if (reg.Cursor == cursor)
                return;

            var impl = (CursorImpl)cursor;
            DebugTools.Assert(impl.Owner == this);

            if (impl.Id == default)
                throw new ObjectDisposedException(nameof(cursor));

            reg.Cursor = impl;
            SendCmd(new CmdWinCursorSet(reg.Sdl2Window, impl.Id));
        }

        private unsafe void WinThreadWinCursorSet(CmdWinCursorSet cmd)
        {
            var window = cmd.Window;
            var ptr = _winThreadCursors[cmd.Cursor].Ptr;

            // TODO: multi-window??
            SDL_SetCursor((SDL_Cursor*) ptr); // todo this is prolly wrong
        }

        private void InitCursors()
        {
            Add(StandardCursorShape.Arrow, SDL_SystemCursor.Arrow);
            Add(StandardCursorShape.IBeam, SDL_SystemCursor.Ibeam);
            Add(StandardCursorShape.Crosshair, SDL_SystemCursor.Crosshair);
            Add(StandardCursorShape.Hand, SDL_SystemCursor.Hand);
            Add(StandardCursorShape.HResize, SDL_SystemCursor.Sizewe);
            Add(StandardCursorShape.VResize, SDL_SystemCursor.Sizens);

            unsafe void Add(StandardCursorShape shape, SDL_SystemCursor sysCursor)
            {
                var id = _clyde.AllocRid();
                var cursor = SDL_CreateSystemCursor(sysCursor);

                var impl = new CursorImpl(this, id, true);
                _standardCursors[(int)shape] = impl;
                _winThreadCursors.Add(id, new WinThreadCursorReg { Ptr = cursor->Handle });  // todo this is prolly wrong
            }
        }

        private sealed class CursorImpl : ICursor
        {
            private readonly bool _standard;
            public SdlWindowingImpl Owner { get; }
            public ClydeHandle Id { get; private set; }

            public CursorImpl(SdlWindowingImpl clyde, ClydeHandle id, bool standard)
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
                Owner.SendCmd(new CmdCursorDestroy(Id));
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

        private void WinThreadCursorDestroy(CmdCursorDestroy cmd)
        {
            // SDL_DestroyCursor(cursor)
        }
    }
}
